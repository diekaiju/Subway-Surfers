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

        // --- Step 3: Patch Missions and UIMissionHelper to be null-safe ---
        MethodDefinition getGameDurationMethod = patchType.Methods.FirstOrDefault(m => m.Name == "GetGameDuration");
        if (getGameDurationMethod == null)
        {
            Console.WriteLine("Error: InputPatch.GetGameDuration method not found.");
            return;
        }
        MethodReference getGameDurationRef = assembly.MainModule.ImportReference(getGameDurationMethod);

        MethodDefinition isGamePausedMethod = patchType.Methods.FirstOrDefault(m => m.Name == "IsGamePaused");
        if (isGamePausedMethod == null)
        {
            Console.WriteLine("Error: InputPatch.IsGamePaused method not found.");
            return;
        }
        MethodReference isGamePausedRef = assembly.MainModule.ImportReference(isGamePausedMethod);

        // Patch Missions.GetMissionInfo()
        TypeDefinition missionsType = assembly.MainModule.GetType("Missions");
        if (missionsType == null)
        {
            Console.WriteLine("Error: Missions class not found.");
            return;
        }

        MethodDefinition getMissionInfoMethod = missionsType.Methods.FirstOrDefault(m => m.Name == "GetMissionInfo" && m.Parameters.Count == 0);
        if (getMissionInfoMethod == null)
        {
            Console.WriteLine("Error: Missions.GetMissionInfo() method not found.");
            return;
        }

        for (int i = 0; i < getMissionInfoMethod.Body.Instructions.Count - 1; i++)
        {
            var inst = getMissionInfoMethod.Body.Instructions[i];
            var next = getMissionInfoMethod.Body.Instructions[i + 1];
            MethodReference mr1 = inst.Operand as MethodReference;
            if (inst.OpCode == OpCodes.Call && mr1 != null && mr1.Name == "get_Instance" && mr1.DeclaringType.Name == "Game")
            {
                MethodReference mr2 = next.Operand as MethodReference;
                if (next.OpCode == OpCodes.Callvirt && mr2 != null && mr2.Name == "GetDuration")
                {
                    inst.Operand = getGameDurationRef;
                    next.OpCode = OpCodes.Nop;
                    next.Operand = null;
                    Console.WriteLine("Patched Missions.GetMissionInfo to use InputPatch.GetGameDuration.");
                    break;
                }
            }
        }

        // Patch UIMissionHelper.LabelAndNumberUpdate()
        TypeDefinition uiMissionHelperType = assembly.MainModule.GetType("UIMissionHelper");
        if (uiMissionHelperType == null)
        {
            Console.WriteLine("Error: UIMissionHelper class not found.");
            return;
        }

        MethodDefinition labelAndNumberUpdateMethod = uiMissionHelperType.Methods.FirstOrDefault(m => m.Name == "LabelAndNumberUpdate");
        if (labelAndNumberUpdateMethod == null)
        {
            Console.WriteLine("Error: UIMissionHelper.LabelAndNumberUpdate method not found.");
            return;
        }

        for (int i = 0; i < labelAndNumberUpdateMethod.Body.Instructions.Count - 1; i++)
        {
            var inst = labelAndNumberUpdateMethod.Body.Instructions[i];
            var next = labelAndNumberUpdateMethod.Body.Instructions[i + 1];
            MethodReference mr1 = inst.Operand as MethodReference;
            if (inst.OpCode == OpCodes.Call && mr1 != null && mr1.Name == "get_Instance" && mr1.DeclaringType.Name == "Game")
            {
                MethodReference mr2 = next.Operand as MethodReference;
                if (next.OpCode == OpCodes.Callvirt && mr2 != null)
                {
                    if (mr2.Name == "get_isPaused")
                    {
                        inst.Operand = isGamePausedRef;
                        next.OpCode = OpCodes.Nop;
                        next.Operand = null;
                        Console.WriteLine("Patched UIMissionHelper.LabelAndNumberUpdate to use InputPatch.IsGamePaused.");
                    }
                    else if (mr2.Name == "GetDuration")
                    {
                        inst.Operand = getGameDurationRef;
                        next.OpCode = OpCodes.Nop;
                        next.Operand = null;
                        Console.WriteLine("Patched UIMissionHelper.LabelAndNumberUpdate to use InputPatch.GetGameDuration.");
                    }
                }
            }
        }

        // Patch DailyWord.Start() to set fallback daily word
        MethodDefinition handleDailyWordFallbackMethod = patchType.Methods.FirstOrDefault(m => m.Name == "HandleDailyWordFallback");
        if (handleDailyWordFallbackMethod == null)
        {
            Console.WriteLine("Error: InputPatch.HandleDailyWordFallback method not found.");
            return;
        }
        MethodReference handleDailyWordFallbackRef = assembly.MainModule.ImportReference(handleDailyWordFallbackMethod);

        TypeDefinition dailyWordType = assembly.MainModule.GetType("DailyWord");
        if (dailyWordType == null)
        {
            Console.WriteLine("Error: DailyWord class not found.");
            return;
        }

        MethodDefinition dailyWordStartMethod = dailyWordType.Methods.FirstOrDefault(m => m.Name == "Start");
        if (dailyWordStartMethod == null)
        {
            Console.WriteLine("Error: DailyWord.Start method not found.");
            return;
        }

        ILProcessor dailyWordIl = dailyWordStartMethod.Body.GetILProcessor();
        Instruction retInst = dailyWordStartMethod.Body.Instructions.FirstOrDefault(inst => inst.OpCode == OpCodes.Ret);
        if (retInst != null)
        {
            Instruction ldarg0dw = dailyWordIl.Create(OpCodes.Ldarg_0);
            Instruction callFallback = dailyWordIl.Create(OpCodes.Call, handleDailyWordFallbackRef);
            
            dailyWordIl.InsertBefore(retInst, ldarg0dw);
            dailyWordIl.InsertBefore(retInst, callFallback);
            Console.WriteLine("Patched DailyWord.Start to use InputPatch.HandleDailyWordFallback.");
        }
        else
        {
            Console.WriteLine("Error: Ret instruction not found in DailyWord.Start.");
        }

        // --- Step 4: Patch Game.DieSequence to call InputPatch.DieSequence ---
        MethodDefinition dieSequenceMethod = gameType.Methods.FirstOrDefault(m => m.Name == "DieSequence");
        if (dieSequenceMethod == null)
        {
            Console.WriteLine("Error: Game.DieSequence method not found.");
            return;
        }

        MethodDefinition patchDieSequence = patchType.Methods.FirstOrDefault(m => m.Name == "DieSequence");
        if (patchDieSequence == null)
        {
            Console.WriteLine("Error: InputPatch.DieSequence method not found.");
            return;
        }

        MethodReference patchDieSequenceRef = assembly.MainModule.ImportReference(patchDieSequence);

        ILProcessor dsIl = dieSequenceMethod.Body.GetILProcessor();
        dieSequenceMethod.Body.Instructions.Clear();
        dieSequenceMethod.Body.Variables.Clear();

        dsIl.Append(dsIl.Create(OpCodes.Ldarg_0));
        dsIl.Append(dsIl.Create(OpCodes.Call, patchDieSequenceRef));
        dsIl.Append(dsIl.Create(OpCodes.Ret));
        Console.WriteLine("Patched Game.DieSequence to redirect to InputPatch.DieSequence.");

        // --- Step 5: Patch Game.StartNewRun to call InputPatch.ResetSaveMeCount ---
        MethodDefinition startNewRunMethod = gameType.Methods.FirstOrDefault(m => m.Name == "StartNewRun");
        if (startNewRunMethod == null)
        {
            Console.WriteLine("Error: Game.StartNewRun method not found.");
            return;
        }

        MethodDefinition resetSaveMeMethod = patchType.Methods.FirstOrDefault(m => m.Name == "ResetSaveMeCount");
        if (resetSaveMeMethod == null)
        {
            Console.WriteLine("Error: InputPatch.ResetSaveMeCount method not found.");
            return;
        }

        MethodReference resetSaveMeRef = assembly.MainModule.ImportReference(resetSaveMeMethod);

        ILProcessor snrIl = startNewRunMethod.Body.GetILProcessor();
        Instruction firstsnr = startNewRunMethod.Body.Instructions[0];
        snrIl.InsertBefore(firstsnr, snrIl.Create(OpCodes.Call, resetSaveMeRef));
        Console.WriteLine("Patched Game.StartNewRun to call InputPatch.ResetSaveMeCount.");

        Console.WriteLine("Saving modified assembly...");
        assembly.Write();
        
        assembly.Dispose();
        patchAssembly.Dispose();

        Console.WriteLine("Patching complete! Subway Surfers has been successfully updated with native controls.");
    }
}
