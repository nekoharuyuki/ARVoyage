using UnityEngine;

namespace Niantic.ARVoyage.SnowballFight
{
    /// <summary>
    /// Used to create random 4-letter session codes in the multiplayer SnowballFight demo.
    /// </summary>
    public static class RandomString
    {
        public static string Generate(int length)
        {
            // to avoid accidental profane words, remove vowels
            // also avoid overly-like sounding consonants
            string[] chars = { "B", "C", "D", "F", "H", "J", "K", "L", "N", "Q", "R", "S", "T", "W", "X", "Z" };
            string str = "";
            for (int i = 0; i < length; i++)
            {
                str += chars[Random.Range(0, chars.Length)];
            }
            return str;
        }
    }
}

