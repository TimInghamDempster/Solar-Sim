using System;
using System.Collections.Generic;
using System.IO;

namespace SlimDXHelpers
{
    /// <summary>
    /// Provides a static method which loads a shader file and
    /// then generates code in it and finally saves it back out
    /// </summary>
    public class ShaderFileEditor
    {
        // Simple text replacement markup which wil
        // be applied to compute shaders before compilation
        public struct MarkupTag
        {
            public string Name { get; }
            public string Value { get; }

            public MarkupTag(string name, object value)
            {
                Name = name ??
                    throw new ArgumentNullException(nameof(name));

                Value = value.ToString() ??
                    throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>
        /// Copy the shader into a new file and replace all
        /// of the tags with the specified values.  Means we
        /// can programatically set things like threads per
        /// group.  Returns the filename of the generated file
        /// </summary>
        public static string GenerateTempFile(string filename, IEnumerable<MarkupTag> markupTags)
        {
            var outputFilename = Path.GetFileNameWithoutExtension(filename);
            outputFilename += "_Generated";
            outputFilename += Path.GetExtension(filename);

            using (StreamReader reader = new StreamReader(filename))
            {
                using (StreamWriter writer = new StreamWriter(outputFilename))
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();

                        foreach (var tag in markupTags)
                        {
                            var fixedUpTag = "#" + tag.Name + "#";
                            line = line.Replace(fixedUpTag, tag.Value);
                        }

                        writer.WriteLine(line);
                    }
                }
            }
            return outputFilename;
        }
    }
}
