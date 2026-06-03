using System;
using System.Reflection;
using System.IO;

class Program
{
    static void Main()
    {
        using (StreamWriter sw = new StreamWriter("inspect_swipe_output.txt"))
        {
            try
            {
                string dllPath = Path.Combine(Directory.GetCurrentDirectory(), "Subway Surfers_Data", "Managed", "Assembly-CSharp.dll");
                Assembly assembly = Assembly.LoadFrom(dllPath);
                
                Type[] targetTypes = new Type[] {
                    assembly.GetType("Swipe"),
                    assembly.GetType("Game"),
                    assembly.GetType("Character"),
                    assembly.GetType("XXInputTest")
                };

                foreach (Type type in targetTypes)
                {
                    if (type == null) continue;
                    sw.WriteLine("========================================");
                    sw.WriteLine("Type: " + type.FullName);
                    sw.WriteLine("========================================");

                    sw.WriteLine("--- Fields ---");
                    foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                    {
                        sw.WriteLine("  {0} {1}", field.FieldType, field.Name);
                    }

                    sw.WriteLine("--- Methods ---");
                    foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                    {
                        sw.WriteLine("  {0} {1}", method.ReturnType, method.Name);
                        // Print parameter types
                        ParameterInfo[] parameters = method.GetParameters();
                        foreach (ParameterInfo p in parameters)
                        {
                            sw.WriteLine("    Param: {0} {1}", p.ParameterType, p.Name);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                sw.WriteLine("Error: " + ex.ToString());
            }
        }
    }
}
