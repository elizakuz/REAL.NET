﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Repo;
namespace WPF_Editor.Models.Interfaces
{
    public interface IScene
    {
        ISceneMediator SceneMediator { get; }

        void HandleLeftSingleClick(object clickInfo);
    }
}
