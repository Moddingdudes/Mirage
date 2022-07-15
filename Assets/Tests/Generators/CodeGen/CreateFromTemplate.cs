using System;
using System.Collections.Generic;
using System.IO;

namespace JamesFrowen.SimpleCodeGen
{
    public sealed class CreateFromTemplate
    {
        private readonly string template;
        private string output;
        private HashSet<string> createdFiles = new HashSet<string>();

        public CreateFromTemplate(string templatePath)
        {
            this.template = File.ReadAllText(templatePath);
            this.output = this.template;
        }

        public void Replace(string oldValue, string newValue)
        {
            this.output = this.output.Replace(oldValue, newValue);
        }
        public void Replace(string oldValue, object newValue)
        {
            this.output = this.output.Replace(oldValue, newValue.ToString());
        }

        public void WriteToFile(string path)
        {
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            if (this.createdFiles.Contains(path))
            {
                throw new ArgumentException($"File already created from this template with same path: {path}");
            }

            this.createdFiles.Add(path);

            File.WriteAllText(path, this.output);
            // reset output to template after writing
            this.output = this.template;
        }
    }
}
