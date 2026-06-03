using System;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

class Program
{
    static void Main()
    {
        string dllPath = Path.Combine("Subway Surfers_Data", "Managed", "Assembly-CSharp.dll");
        AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(dllPath);
        TypeDefinition type = assembly.MainModule.GetType("Running");
        
        using (StreamWriter sw = new StreamWriter("double_tap_il.txt"))
        {
            if (type == null)
            {
                sw.WriteLine("Could not find type 'Running'");
                return;
            }

            foreach (MethodDefinition method in type.Methods)
            {
                if (method.Name == "HandleDoubleTap")
                {
                    sw.WriteLine("========================================");
                    sw.WriteLine("Method: " + method.Name);
                    sw.WriteLine("========================================");
                    if (method.HasBody)
                    {
                        foreach (Instruction inst in method.Body.Instructions)
                        {
                            sw.WriteLine("{0}: {1} {2}", inst.Offset, inst.OpCode, inst.Operand);
                        }
                    }
                }
            }
        }
    }
}
