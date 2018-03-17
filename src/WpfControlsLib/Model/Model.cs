﻿/* Copyright 2017-2018 REAL.NET group
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License. */

namespace WpfControlsLib.Model
{
    using System;
    using WpfControlsLib.ViewModel;

    /// <summary>
    /// Model in MVC architecture. Wraps repository, provides operations like adding or removing models, edges and
    /// nodes (note that "model" is used with two different meanings around the project -- a visual model consisting
    /// of nodes and edges and a model in MVC sense) and notifications for all concerned tools about changes in repo.
    /// This class is a ground truth about visual model currently edited and is supposed to be used by all tools and
    /// parts of an editor who need to listen for visual model changes and/or modify visual model.
    /// </summary>
    public class Model
    {
        public Model()
        {
            this.Repo = global::Repo.RepoFactory.CreateRepo();
            this.Constraints = new WpfControlsLib.Constraints.Constraints();
        }

        public event EventHandler<VertexEventArgs> NewVertexInRepo;

        public event EventHandler<EdgeEventArgs> NewEdgeInRepo;

        public WpfControlsLib.Constraints.Constraints Constraints { get; set; }

        public string ModelName { get; set; }

        public string ErrorMsg { get; set; }

        public Repo.IRepo Repo { get; }

        // TODO name
        public bool ConstraintsCheck()
        {
            if (this.Constraints.CheckEdges(this.Repo.Model(this.ModelName).Edges))
            {
                this.ErrorMsg = "Number of edges exceeds allowed.";
                return false;
            }

            if (this.Constraints.CheckNodes(this.Repo.Model(this.ModelName).Nodes))
            {
                this.ErrorMsg = "Number of nodes exceeds allowed.";
                return false;
            }

            return true;
        }

        public void NewNode(Repo.IElement element, string modelName)
        {
            if (this.Constraints.AllowCreateNode(this.Repo.Model(this.ModelName).Nodes))
            {
                var model = this.Repo.Model(modelName);
                var newNode = model.CreateElement(element) as Repo.INode;
                this.RaiseNewVertexInRepo(newNode);
            }
            else
            {
                // TODO
            }
        }

        public void NewEdge(Repo.IEdge edge, NodeViewModel prevVer, NodeViewModel ctrlVer)
        {
            if (this.Constraints.AllowCreateEdge(this.Repo.Model(this.ModelName).Edges))
            {
                this.RaiseNewEdgeInRepo(edge, prevVer, ctrlVer);
            }
            else
            {
                // TODO
            }
        }

        private void RaiseNewVertexInRepo(Repo.INode node)
        {
            var args = new VertexEventArgs
            {
                Node = node
            };
            this.NewVertexInRepo?.Invoke(this, args);
        }

        private void RaiseNewEdgeInRepo(Repo.IEdge edge, NodeViewModel prevVer, NodeViewModel ctrlVer)
        {
            var args = new EdgeEventArgs
            {
                Edge = edge,
                PrevVer = prevVer,
                CtrlVer = ctrlVer
            };
            this.NewEdgeInRepo?.Invoke(this, args);
        }

        public class VertexEventArgs : EventArgs
        {
            public Repo.INode Node { get; set; }
        }

        public class EdgeEventArgs : EventArgs
        {
            public Repo.IEdge Edge { get; set; }

            public NodeViewModel PrevVer { get; set; }

            public NodeViewModel CtrlVer { get; set; }
        }
    }
}
