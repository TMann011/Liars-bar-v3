#r @"C:\Program Files (x86)\Steam\steamapps\common\Liar's Bar\MelonLoader\net6\Mono.Cecil.dll"
open Mono.Cecil

let asm = AssemblyDefinition.ReadAssembly(@"C:\Program Files (x86)\Steam\steamapps\common\Liar's Bar\MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll")
let dk = asm.MainModule.GetType("Il2Cpp.DeckGameplay")
for m in dk.Methods do
    if m.Name.Contains("currentRevolver") || m.Name.Contains("revolverBullet") then
        printfn "Deck Method: %s %s" m.ReturnType.Name m.Name
