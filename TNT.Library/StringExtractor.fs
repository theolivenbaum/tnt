/// With Mono.Cecil we extract strings from assemblies that are 
/// marked with functions in TNT.CSharp or TNT.FSharp.
module TNT.Library.StringExtractor

open Mono.Cecil
open Mono.Cecil.Cil
open TNT.Model

[<AutoOpen>]
module private Private = 

    let TFunctions = [|
        "System.String TNT.CSharp.Extensions::t(System.String)"
        "System.String TNT.FSharp.Extensions::String.get_t(System.String)"
    |]

    let extractFromInstructions (instructions : Instruction seq) = 

        let isTranslationCall (instruction: Instruction) = 
            match instruction.OpCode with
            | op when op = OpCodes.Call -> 
                match instruction.Operand with
                | :? MethodReference as md ->
                    TFunctions |> Array.contains md.FullName
                | _ -> false
            | _ -> false
            

        instructions
        |> Seq.map ^ fun instruction -> 
            match instruction.OpCode with
            | op when op = OpCodes.Ldstr 
                && instruction.Next <> null 
                && isTranslationCall instruction.Next
                -> Some ^ string instruction.Operand
            | _ -> None
        |> Seq.choose id

let extract (name: AssemblyPath) :  string list = 

    let assemblyDefinition = AssemblyDefinition.ReadAssembly(string name)

    let rec extractFromType (typeDefinition: TypeDefinition) : string seq = seq {
        yield!
            typeDefinition.NestedTypes
            |> Seq.map ^ fun nestedTypeDefinition ->
                extractFromType nestedTypeDefinition
            |> Seq.collect id
        yield!
            typeDefinition.Methods
            |> Seq.map ^ fun methodDefinition ->
                methodDefinition.Body.Instructions
                |> extractFromInstructions
            |> Seq.collect id
    }

    assemblyDefinition.Modules
    |> Seq.map ^ fun moduleDefinition ->
        moduleDefinition.Types
        |> Seq.map ^ extractFromType
        |> Seq.collect id
    |> Seq.collect id
    // ensure reproducibility and remove duplicates
    |> Seq.sort |> Seq.distinct
    |> Seq.toList

    

    

    
    
