#r @"C:\Program Files (x86)\Steam\steamapps\common\Liar's Bar\MelonLoader\net6\Mono.Cecil.dll"
open Mono.Cecil

let asm = AssemblyDefinition.ReadAssembly(@"C:\Program Files (x86)\Steam\steamapps\common\Liar's Bar\MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll")
let poc = asm.MainModule.GetType("Il2Cpp.PlayerObjectController")
for m in poc.Methods do
    if m.Name = "UserCode_CmdSetPlayerName__String" || m.Name = "PlayerNameUpdate" then
        printfn "Method: %s" m.Name
