using System;

namespace DotDart
{
    /// <summary>
    /// Still researching this.
    /// </summary>
    public static class DartCore
    {
        public static Action<string> printToZone = null;
        public static void print(object obj)
        {
            var line = obj.ToString();
            if (printToZone == null) {
                Console.WriteLine(line);
            } else {
                printToZone(line);
            }
        }
    }
}