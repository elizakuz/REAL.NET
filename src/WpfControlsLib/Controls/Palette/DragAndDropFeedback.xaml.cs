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

using System.Windows;

namespace WpfControlsLib.Controls.Palette
{
    /// <summary>
    /// Drag-and-drop feedback window, shows element dragged from palette.
    /// </summary>
    public partial class DragAndDropFeedback : Window
    {
        /// <summary>
        /// Gets or sets an image drawn under cursor.
        /// </summary>
        public string Image { get; set; }

        /// <summary>
        /// Creates a new instance of the <see cref="DragAndDropFeedback"/> class.
        /// </summary>
        public DragAndDropFeedback()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }
    }
}
