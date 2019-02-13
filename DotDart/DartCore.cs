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

        /// <summary>
        /// Dart can return anything from 'main', including 'dynamic'.
        /// C# may only return int and void.
        /// </summary>
        public static void _special_script_dart_return(Type returnType, object returnValue)
        {
            // todo: pipe somehow so it can be easily retrieved
            print($"Return value = {returnType}:{returnValue};");
            Environment.Exit(0);
        }
    }
}