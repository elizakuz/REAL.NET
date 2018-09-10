/* Copyright 2017-2018 REAL.NET group
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

namespace SmartGreenhouse.View
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Controls;
    using System.CodeDom.Compiler;
    using System.Diagnostics;
    using Microsoft.CSharp;
    using EditorPluginInterfaces;
    using PluginManager;
    using Repo;
    using Generator;
    using WpfControlsLib.Constraints;
    using WpfControlsLib.Controls.Console;
    using WpfControlsLib.Controls.ModelSelector;
    using WpfControlsLib.Controls.Scene;
    using WpfControlsLib.Controls.Toolbar;
    using Palette = WpfControlsLib.Controls.Palette.Palette;
    
    /// <summary>
    /// Main window of the application, launches on application startup.
    /// </summary>
    internal partial class MainWindow : INotifyPropertyChanged
    {
        private readonly WpfControlsLib.Model.Model model;
        private readonly WpfControlsLib.Controller.Controller controller;

        public event PropertyChangedEventHandler PropertyChanged;

        public AppConsoleViewModel Console { get; } = new AppConsoleViewModel();

        //public ToolbarViewModel Toolbar { get; } = new ToolbarViewModel();

        public string WindowTitle
        {
            get
            {
                var fileName = (model == null || model.CurrentFileName == "")
                    ? "(unsaved)"
                    : model.CurrentFileName;
                var unsavedChanges = model?.HasUnsavedChanges == true ? "*" : "";
                return $"REAL.NET {fileName} {unsavedChanges}";
            }
        }

        public MainWindow()
        {
            // TODO: Fix sequential coupling here.
            this.DataContext = this;
            this.InitializeComponent();

            this.model = new WpfControlsLib.Model.Model();

            this.model.Reinit += this.Reinit;
            this.model.FileSaved += (_, __) => NotifyTitleChanged();
            this.model.UnsavedChanges += (_, __) => NotifyTitleChanged();

            // Notifying window first time, to initialize title.
            NotifyTitleChanged();

            this.palette.SetModel(this.model);

            this.controller = new WpfControlsLib.Controller.Controller();

            this.Closed += this.CloseChildrenWindows;

            this.scene.ElementManipulationDone += (sender, args) => this.palette.ClearSelection();
            this.scene.ElementAdded += (sender, args) => this.modelExplorer.NewElement(args.Element);
            this.scene.ElementRemoved += (sender, args) => this.modelExplorer.RemoveElement(args.Element);
            this.scene.NodeSelected += (sender, args) => this.attributesView.DataContext = args.Node;
            this.scene.EdgeSelected += (sender, args) => this.attributesView.DataContext = args.Edge;

            this.scene.Init(this.model, this.controller, new PaletteAdapter(this.palette));

            /*this.modelSelector.Init(this.model);
            this.modelSelector.ChangeModel(0);*/

            this.SelectModel("GreenhouseTestModel");

            this.InitAndLaunchPlugins();
            //this.InitToolbar();
        }

        private void Reinit(object sender, EventArgs e)
        {
            //this.SelectModel(this.modelSelector.ModelNames[0]);
            this.SelectModel("GreenhouseTestModel");
        }

        private void OnModelSelectionChanged(object sender, ModelSelector.ModelSelectedEventArgs args)
        {
            SelectModel(args.ModelName);
        }

        private void SelectModel(string modelName)
        {
            this.scene.Clear();
            this.modelExplorer.Clear();
            this.model.ModelName = modelName;
            this.palette.InitPalette(this.model.ModelName);
            this.scene.Reload();
        }

        /*private void InitToolbar()
        {
            this.Console.Messages.Add("Initializing ToolBar");
            var sample = new WpfControlsLib.Controls.Toolbar.StandardButtonsAndMenus.SampleButtonsCollection(this.Console, this.controller);
            var buttons = sample.SampleButtons;
            foreach (var button in buttons)
            {
                this.Toolbar.AddButton(button);
            }
        }*/

        private void InitAndLaunchPlugins()
        {
            var libs = new PluginLauncher<PluginConfig>();
            const string folder = "../../../plugins";
            var pluginDirs = new List<string>(System.IO.Directory.GetDirectories(folder));
            foreach (var plugindir in pluginDirs)
            {
                var dirs = new List<string>(System.IO.Directory.GetDirectories(plugindir + "/bin"));
                var config = new PluginConfig(this.model, null, null, this.Console, null);
                foreach (var dir in dirs)
                {
                    libs.LaunchPlugins(dir, config);
                }
            }
        }

        private void CloseChildrenWindows(object sender, EventArgs e)
        {
            foreach (Window w in Application.Current.Windows)
            {
                w.Close();
            }
        }

        private void ConstraintsButtonClick(object sender, RoutedEventArgs e)
        {
            var constraintsWindow = new ConstraintsWindow(this.model);
            constraintsWindow.ShowDialog();

            if (!this.model.ConstraintsCheck())
            {
                this.Console.ReportError(this.model.Constraints.ErrorMsg);
            }
        }

        private void AttributesViewCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
            => this.scene.ChangeEdgeLabel(((TextBox)e.EditingElement).Text);
        
        private class PaletteAdapter : IElementProvider
        {
            private readonly Palette palette;

            internal PaletteAdapter(Palette palette)
            {
                this.palette = palette;
            }

            public IElement Element => this.palette.SelectedElement;
        }

        private void NotifyTitleChanged() => this.PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(nameof(this.WindowTitle))
            );

        private void OnCanExecuteForAlwaysExecutable(object sender, System.Windows.Input.CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void OnNewExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            this.model.New();
            this.controller.ClearHistory();
            NotifyTitleChanged();
        }

        private void CleanButtonClick(object sender, RoutedEventArgs e)
        {
            //this.SelectModel("GreenhouseTestModel");
            this.scene.Clear();
        }

            private void GenerateButtonClick(object sender, RoutedEventArgs e)
        {
            var scenario = new Scenario { r = this.model.Repo };
            string code = scenario.TransformText();
            File.WriteAllText(@"C:\Users\User\Desktop\a.txt", code);

            CompilerParameters parameters = new CompilerParameters();
            parameters.GenerateExecutable = true;
            //parameters.GenerateInMemory = true;
            parameters.OutputAssembly = "greenhouse.exe";

            parameters.ReferencedAssemblies.Add("System.Core.dll");
            parameters.ReferencedAssemblies.Add("System.Reactive.dll");
            parameters.ReferencedAssemblies.Add("Trik.Core.dll");
            parameters.ReferencedAssemblies.Add("RobotSimulation.dll");
            parameters.ReferencedAssemblies.Add("Generator.dll");

            var compiler = new CSharpCodeProvider();
            var results = compiler.CompileAssemblyFromSource(parameters, code);

            Process.Start("greenhouse.exe");

            /*if (results.Errors.Count > 0)
            {
                foreach (CompilerError CompErr in results.Errors)
                {
                    System.Console.WriteLine("Line number " + CompErr.Line +
                                ", Error Number: " + CompErr.ErrorNumber +
                                ", '" + CompErr.ErrorText + ";" +
                                Environment.NewLine + Environment.NewLine);
                }
            }
            else
            {
                Process.Start("greenhouse.exe");
            }*/

            /*Process winscp = new Process();
            winscp.StartInfo.FileName = @"C:\Program Files (x86)\WinSCP\winscp.com";
            winscp.StartInfo.RedirectStandardInput = true;
            winscp.StartInfo.UseShellExecute = false;
            winscp.StartInfo.CreateNoWindow = false;
            winscp.Start();

            winscp.StandardInput.WriteLine("option batch abort");
            winscp.StandardInput.WriteLine("option confirm off");

            winscp.StandardInput.WriteLine("open scp://root:@192.168.77.1:22");
            winscp.StandardInput.WriteLine(@"put greenhouse.exe /home/root/abc/");
            winscp.StandardInput.WriteLine("exit");

            winscp.StandardInput.Close();
            winscp.WaitForExit();

            Process psi = new Process();
            psi.StartInfo.FileName = @"C:\Windows\System32\cmd";
            psi.StartInfo.RedirectStandardInput = true;
            psi.StartInfo.RedirectStandardOutput = true;
            psi.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            psi.StartInfo.UseShellExecute = false;
            psi.StartInfo.CreateNoWindow = false;
            psi.Start();

            psi.StandardInput.WriteLine("plink root@192.168.77.1:22");
            psi.StandardInput.WriteLine("cd abc");
            psi.StandardInput.WriteLine("mono greenhouse.exe");

            while (true)
            {
                System.Console.WriteLine(psi.StandardOutput.ReadLine());
            }*/
        }

        private void OnOpenExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".rns",
                Filter = "Real.NET Saves (.rns)|*.rns"
            };

            var result = dialog.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                model.Open(dialog.FileName);
                this.controller.ClearHistory();
                NotifyTitleChanged();
            }
        }

        private void OnSaveExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            if (this.model.CurrentFileName == string.Empty)
            {
                this.SaveAs();
            }
            else
            {
                this.model.Save();
            }
        }

        private void OnSaveAsExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            SaveAs();
        }

        private void OnQuitExecuted(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            this.Close();
        }

        private void SaveAs()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                DefaultExt = ".rns",
                Filter = "Real.NET Saves|*.rns"
            };

            var result = dialog.ShowDialog();

            // Get the selected file name and display in a TextBox
            if (result == true)
            {
                model.SaveAs(dialog.FileName);
            }
        }
    }
}