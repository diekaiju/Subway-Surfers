using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

class Program
{
    static void Main()
    {
        string managedDir = Path.Combine("Subway Surfers_Data", "Managed");
        string assemblyPath = Path.Combine(managedDir, "Assembly-CSharp.dll");
        string patchDllPath = Path.Combine(managedDir, "InputPatch.dll");
        
        string backupPath = assemblyPath + ".bak";
        
        // 1. Ensure backup exists
        if (!File.Exists(backupPath))
        {
            File.Copy(assemblyPath, backupPath);
            Console.WriteLine("Created clean backup of Assembly-CSharp.dll at: " + backupPath);
        }
        else
        {
            // Always restore from backup to start from a clean state
            File.Copy(backupPath, assemblyPath, true);
            Console.WriteLine("Restored original Assembly-CSharp.dll from backup.");
        }

        if (!File.Exists(patchDllPath))
        {
            Console.WriteLine("Error: InputPatch.dll not found in Managed directory.");
            return;
        }

        Console.WriteLine("Loading assemblies...");
        AssemblyDefinition assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters { ReadWrite = true });
        AssemblyDefinition patchAssembly = AssemblyDefinition.ReadAssembly(patchDllPath);

        TypeDefinition gameType = assembly.MainModule.GetType("Game");
        if (gameType == null)
        {
            Console.WriteLine("Error: Game class not found in assembly.");
            return;
        }

        MethodDefinition handleControlsMethod = gameType.Methods.FirstOrDefault(m => m.Name == "HandleControls");
        if (handleControlsMethod == null)
        {
            Console.WriteLine("Error: Game.HandleControls method not found.");
            return;
        }

        // Import InputPatch.HandleControls method
        TypeDefinition patchType = patchAssembly.MainModule.GetType("InputPatch");
        if (patchType == null)
        {
            Console.WriteLine("Error: InputPatch class not found in InputPatch.dll.");
            return;
        }

        MethodDefinition patchMethod = patchType.Methods.FirstOrDefault(m => m.Name == "HandleControls");
        if (patchMethod == null)
        {
            Console.WriteLine("Error: InputPatch.HandleControls method not found.");
            return;
        }

        MethodReference patchMethodRef = assembly.MainModule.ImportReference(patchMethod);

        // --- Step 1: Inject InputPatch.HandleControls(this) ---
        ILProcessor il = handleControlsMethod.Body.GetILProcessor();
        
        // Find first branch instruction (which branches over the pause check)
        Instruction branchInst = null;
        foreach (var inst in handleControlsMethod.Body.Instructions)
        {
            if (inst.OpCode == OpCodes.Brfalse || inst.OpCode == OpCodes.Brfalse_S)
            {
                branchInst = inst;
                break;
            }
        }

        if (branchInst == null)
        {
            Console.WriteLine("Error: Could not find branch instruction in HandleControls.");
            return;
        }

        Instruction targetInst = (Instruction)branchInst.Operand;
        Console.WriteLine("Found branch target instruction: " + targetInst);

        // Create injection instructions
        Instruction ldarg0 = il.Create(OpCodes.Ldarg_0);
        Instruction callPatch = il.Create(OpCodes.Call, patchMethodRef);

        // Insert before targetInst
        il.InsertBefore(targetInst, ldarg0);
        il.InsertBefore(targetInst, callPatch);

        // Update branch target to the start of our injected instructions
        branchInst.Operand = ldarg0;
        Console.WriteLine("Injected InputPatch.HandleControls call and updated branch target.");

        // --- Step 2: Disable FillYValues() and AnalyzeTilt() ---
        Instruction callFillY = null;
        Instruction callAnalyzeTilt = null;

        foreach (var inst in handleControlsMethod.Body.Instructions)
        {
            MethodReference mr = inst.Operand as MethodReference;
            if (inst.OpCode == OpCodes.Call && mr != null)
            {
                if (mr.Name == "FillYValues")
                {
                    callFillY = inst;
                }
                else if (mr.Name == "AnalyzeTilt")
                {
                    callAnalyzeTilt = inst;
                }
            }
        }

        if (callFillY != null)
        {
            Instruction prev = callFillY.Previous;
            if (prev != null && prev.OpCode == OpCodes.Ldarg_0)
            {
                prev.OpCode = OpCodes.Nop;
                prev.Operand = null;
            }
            callFillY.OpCode = OpCodes.Nop;
            callFillY.Operand = null;
            Console.WriteLine("Disabled FillYValues call.");
        }
        else
        {
            Console.WriteLine("Warning: FillYValues call not found.");
        }

        if (callAnalyzeTilt != null)
        {
            Instruction prev = callAnalyzeTilt.Previous;
            if (prev != null && prev.OpCode == OpCodes.Ldarg_0)
            {
                prev.OpCode = OpCodes.Nop;
                prev.Operand = null;
            }
            Instruction next = callAnalyzeTilt.Next;
            if (next != null && next.OpCode == OpCodes.Pop)
            {
                next.OpCode = OpCodes.Nop;
                next.Operand = null;
            }
            callAnalyzeTilt.OpCode = OpCodes.Nop;
            callAnalyzeTilt.Operand = null;
            Console.WriteLine("Disabled AnalyzeTilt call.");
        }
        else
        {
            Console.WriteLine("Warning: AnalyzeTilt call not found.");
        }

        Console.WriteLine("Saving modified assembly...");
        assembly.Write();
        
        assembly.Dispose();
        patchAssembly.Dispose();

        Console.WriteLine("Patching complete! Subway Surfers has been successfully updated with native controls.");
    }
}
