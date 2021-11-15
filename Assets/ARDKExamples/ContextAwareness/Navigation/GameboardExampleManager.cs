// Copyright 2021 Niantic, Inc. All Rights Reserved.

using System.Collections;
using System.Collections.Generic;

using Niantic.ARDK.Extensions.Meshing;
using Niantic.ARDKExamples.Gameboard;

using UnityEngine;
using UnityEngine.UI;

namespace Niantic.ARDKExamples
{
    public class GameboardExampleManager: MonoBehaviour
    {
#pragma warning disable 0649
        [SerializeField]
        private Camera _arCamera;

        [SerializeField]
        private ARMeshManager _meshManager;

        [Header("Gameboard Configuration")]
        [SerializeField]
        private MeshFilter _walkablePlaneMesh;

        [SerializeField]
        private GameObject _actorPrefab;

        [SerializeField]
        private float _scanInterval = 0.2f;

        [SerializeField]
        private LayerMask _raycastLayerMask;

        [Header("UI")]
        [SerializeField]
        private Button _replaceButton;

        [SerializeField]
        private Text _replaceButtonText;

        [SerializeField]
        private Button _callButton;

#pragma warning restore 0649

        private IGameBoard _gameBoard;
        private GameObject _actor;
        private Surface _surfaceToRender;
        private float _lastScan;
        private bool _isReplacing;
        private Coroutine _actorMoveCoroutine;
        private Coroutine _actorJumpCoroutine;
        private List<Waypoint> _lastPath;

        public void StopAR()
        {
            _meshManager.ClearMeshObjects();
            _walkablePlaneMesh.mesh = null;
            _surfaceToRender = null;

            Destroy(_actor);
            _actor = null;

            if (_actorMoveCoroutine != null)
            {
                StopCoroutine(_actorMoveCoroutine);
                _actorMoveCoroutine = null;
            }

            if (_actorJumpCoroutine != null)
            {
                StopCoroutine(_actorJumpCoroutine);
                _actorJumpCoroutine = null;
            }

            _replaceButtonText.text = "Place";

            _replaceButton.interactable = false;
            _callButton.interactable = false;

            _isReplacing = false;
        }

        private void Awake()
        {
            // Get typical settings for the game board
            var settings = GameBoard.DefaultSettings;

            // Assign the layer of the mesh
            settings.LayerMask = _raycastLayerMask;

            // Allocate the game board
            _gameBoard = new GameBoard(settings);

            // Allocate mesh to render walkable surfaces
            _walkablePlaneMesh.mesh = new Mesh();
            _walkablePlaneMesh.mesh.MarkDynamic();

            _callButton.interactable = false;
            _replaceButton.interactable = false;
            _replaceButtonText.text = "Place";
        }

        private void OnEnable()
        {
            _replaceButton.onClick.AddListener(ReplaceButton_OnClick);
            _callButton.onClick.AddListener(CallButton_OnClick);
        }

        private void OnDisable()
        {
            _replaceButton.onClick.RemoveListener(ReplaceButton_OnClick);
            _callButton.onClick.RemoveListener(CallButton_OnClick);
        }

        private void Update()
        {
            if (_isReplacing)
            {
                HandlePlacement();
            }
            else
            {
                HandleScanning();
            }

            // Render the surface
            if (_surfaceToRender != null)
            {
                _gameBoard.UpdateSurfaceMesh(_surfaceToRender, _walkablePlaneMesh.mesh);
            }
        }

        private void OnDrawGizmos()
        {
            if (_lastPath == null || _actorMoveCoroutine == null)
            {
                return;
            }

            for (var i = 0; i < _lastPath.Count; i++)
            {
                var position = _lastPath[i].WorldPosition;
                Gizmos.DrawSphere(position, 0.05f);
                Gizmos.DrawLine
                    (_lastPath[i].WorldPosition, _lastPath[Mathf.Clamp(i + 1, 0, _lastPath.Count - 1)].WorldPosition);
            }
        }

        private void HandlePlacement()
        {
            Vector3 hitPoint;
            var cameraTransform = _arCamera.transform;
            var ray = new Ray(cameraTransform.position, cameraTransform.forward);

            if (_gameBoard.RayCast(ray, out _surfaceToRender, out hitPoint))
            {
                if (_gameBoard.CanFitObject(center: hitPoint, extent: 0.2f))
                {
                    _actor.transform.position = hitPoint;
                    _replaceButton.interactable = true;
                }
            }
        }

        private void HandleScanning()
        {
            if (!(Time.time - _lastScan > _scanInterval))
            {
                return;
            }

            _lastScan = Time.time;
            var cameraTransform = _arCamera.transform;
            var playerPosition = cameraTransform.position;
            var playerForward = cameraTransform.forward;

            // The origin of the scan should be in front of the player
            var origin = playerPosition +
                Vector3.ProjectOnPlane(playerForward, Vector3.up).normalized;

            // Scan in a 75 cm radius
            _gameBoard.Scan(origin, radius: 0.75f);

            // Raycast the game board to get the surface the player is looking at
            var ray = new Ray(playerPosition, playerForward);
            Vector3 hit;
            _gameBoard.RayCast(ray, out _surfaceToRender, out hit);

            // Only allow placing the actor if at least one surface is discovered
            _replaceButton.interactable = _gameBoard.NumberOfPlanes > 0;
        }

        private IEnumerator Move(Transform actor, IList<Waypoint> path, float speed = 3.0f)
        {
            var startPosition = actor.position;
            var interval = 0.0f;
            var destIdx = 0;

            while (destIdx < path.Count)
            {
                interval += Time.deltaTime * speed;
                actor.position = Vector3.Lerp(startPosition, path[destIdx].WorldPosition, interval);
                if (Vector3.Distance(actor.position, path[destIdx].WorldPosition) < 0.01f)
                {
                    startPosition = actor.position;
                    interval = 0;
                    destIdx++;

                    // Do we need to jump?
                    if (destIdx < path.Count && path[destIdx].Type == Waypoint.MovementType.SurfaceEntry)
                    {
                        yield return new WaitForSeconds(0.5f);

                        _actorJumpCoroutine = StartCoroutine
                        (
                            Jump(actor, actor.position, path[destIdx].WorldPosition)
                        );

                        yield return _actorJumpCoroutine;

                        _actorJumpCoroutine = null;
                        startPosition = actor.position;
                        destIdx++;
                    }
                }

                yield return null;
            }

            _actorMoveCoroutine = null;
        }

        private IEnumerator Jump(Transform actor, Vector3 from, Vector3 to, float speed = 2.0f)
        {
            var interval = 0.0f;
            var height = Mathf.Max(0.1f, Mathf.Abs(to.y - from.y));
            while (interval < 1.0f)
            {
                interval += Time.deltaTime * speed;
                var p = Vector3.Lerp(from, to, interval);
                actor.position = new Vector3
                (
                    p.x,
                    -4.0f * height * interval * interval +
                    4.0f * height * interval +
                    Mathf.Lerp(from.y, to.y, interval),
                    p.z
                );

                yield return null;
            }

            actor.position = to;
        }

        public void ClearBoard()
        {
            _gameBoard.Clear();
        }

        private void ReplaceButton_OnClick()
        {
            if (_actor == null)
            {
                _actor = Instantiate(_actorPrefab);
            }

            _isReplacing = !_isReplacing;
            _replaceButtonText.text = _isReplacing ? "Done" : "Replace";
            _replaceButton.interactable = !_isReplacing;
            _callButton.interactable = !_isReplacing;
        }

        private void CallButton_OnClick()
        {
            _lastPath = _gameBoard.CalculatePath(_actor.transform.position, _arCamera.transform.position, PathFindingBehaviour.InterSurfacePreferResults);

            if (_actorMoveCoroutine != null)
                StopCoroutine(_actorMoveCoroutine);

            if (_actorJumpCoroutine != null)
                StopCoroutine(_actorJumpCoroutine);

            _actorMoveCoroutine = StartCoroutine(Move(_actor.transform, _lastPath));
        }
    }
}