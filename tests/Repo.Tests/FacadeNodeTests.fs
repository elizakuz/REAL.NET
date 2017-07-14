﻿(* Copyright 2017 Yurii Litvinov
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. *)

module FacadeNodeTest

open NUnit.Framework
open FsUnit

open RepoExperimental
open RepoExperimental.FacadeLayer

[<Test>]
let ``Node in a model shall have metatype`` () =
    let repo = RepoFactory.CreateRepo ()

    let model = repo.Models |> Seq.find (fun m -> m.Name = "RobotsTestModel")
    let dataLayerModel = (model :?> Model).UnderlyingModel

    let metamodel = repo.Models |> Seq.find (fun m -> m.Name = "RobotsMetamodel")
    let dataLayerMetamodel = (metamodel :?> Model).UnderlyingModel

    let dataLayerClass = dataLayerMetamodel.Nodes |> Seq.find (fun n -> n.Name = "MotorsForward") :> DataLayer.IElement
    let dataLayerElement = dataLayerModel.Nodes |> Seq.find (fun n -> n.Class = dataLayerClass)

    let attributeRepository = AttributeRepository ()
    let elementRepository = ElementRepository (dataLayerModel, attributeRepository) :> IElementRepository

    let motorsForwardInstanceNode = elementRepository.GetElement dataLayerElement Metatype.Node
    let motorsForwardTypeNode = elementRepository.GetElement dataLayerClass Metatype.Node

    motorsForwardInstanceNode.Metatype |> should equal Metatype.Node
