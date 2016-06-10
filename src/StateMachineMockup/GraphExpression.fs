﻿module GraphExpression

type Node =
    { Name : string
      Inputs : (Node*int) list
      OutputRank : int }

type Graph =
    { Nodes : Node list 
      Target : Node option }

type GraphBuilder() =
    
    member x.Bind(expr: Graph, follow: Node -> Graph) : Graph =
        let followGraph = follow (expr.Target.Value)
        let allNodes = expr.Nodes @ followGraph.Nodes |> List.distinct
        { Nodes = allNodes; Target = followGraph.Target }

    member x.ReturnFrom(expr: Graph) = expr

    member x.Return(_:unit) = { Nodes = []; Target = None }

let graph = GraphBuilder()

let node1 name inNodes =
    let node = { Name = name; Inputs = inNodes |> List.map(fun n -> n,0); OutputRank = 0 }
    { Nodes = node :: inNodes; Target = Some node}

let scatter_node1 name inNodes =
    let node = { Name = name; Inputs = inNodes |> List.map(fun n -> n,0); OutputRank = 1 }
    { Nodes = node :: inNodes; Target = Some node}

let reduce_node1 name inNode =
    let node = { Name = name; Inputs = [inNode,1]; OutputRank = 1 }
    { Nodes = [node; inNode]; Target = Some node}


type internal Vertex = StateMachineMockup.Vertex
type internal Dag = Angara.Graph.DataFlowGraph<Vertex>

let rec internal buildType rank =
    if rank = 0 then typeof<int>
    else System.Array.CreateInstance(buildType (rank-1), 0).GetType()

let buildDag (expr: Graph) =
    let dag, nodeToVertex = expr.Nodes |> Seq.fold(fun (dag:Dag, nodeToVertex:Map<string, Vertex>) node -> 
        let v = Vertex(node.Name, node.Inputs |> List.map(fun (_,inputRank) -> buildType inputRank), [buildType node.OutputRank])
        dag.Add v, nodeToVertex.Add(node.Name, v)) (Dag(), Map.empty<string, Vertex>)

    let dag = expr.Nodes |> Seq.fold(fun (dag:Dag) node -> 
        node.Inputs 
        |> Seq.mapi(fun i inp -> i,inp) 
        |> Seq.fold(fun dag (inpRef, (input: Node, _)) -> dag.Connect (nodeToVertex.[node.Name], inpRef) (nodeToVertex.[input.Name], 0)) dag) dag
    
    dag, nodeToVertex