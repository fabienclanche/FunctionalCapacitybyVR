using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace Utils
{ 
    public sealed class CollectionUtils
    {
        public static readonly Color BLUE_COLOR = new Color(28 / 255f, 252 / 255f, 253 / 255f);
        public static readonly Color GREEN_COLOR = new Color(28 / 255f, 253 / 255f, 59 / 255f);

        private CollectionUtils()
        {

        }

        public static string ToString<T>(IEnumerable<T> e)
        {
            return "[" + e.Select(x => x + "").Aggregate((x, y) => (x + ", " + y)) + "]";
        }
    }
}

