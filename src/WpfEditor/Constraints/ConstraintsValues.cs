﻿namespace WpfEditor.Constraints
{
    public class ConstraintsValues
    {
        public string ElementType { get; set; }

        public string MakeString()
        {
            return this.ElementType.ToString();
        }
    }
}