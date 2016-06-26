using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Meziantou.HtmlLocalizer
{
    public class Project
    {
        [JsonIgnore]
        public DirectoryInfo BaseDirectory { get; set; }

        public ProjectOptions Options { get; } = new ProjectOptions();
        public IList<HtmlFile> Files { get; } = new List<HtmlFile>();

        public string Save()
        {
            using (var sw = new StringWriter())
            {
                Save(sw);
                return sw.ToString();
            }
        }

        public void Save(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            using (var stream = File.OpenWrite(path))
            {
                Save(stream);
            }
        }

        public void Save(Stream stream)
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                Save(writer);
            }
        }

        public virtual void Save(TextWriter writer)
        {
            if (writer == null) throw new ArgumentNullException(nameof(writer));

            var serializer = CreateJsonSerializer();
            serializer.Serialize(writer, this);
        }

        public void Load(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            using (var stream = File.OpenRead(path))
            {
                if (BaseDirectory == null)
                {
                    var directoryName = Path.GetDirectoryName(path);
                    BaseDirectory = new DirectoryInfo(directoryName);
                }

                Load(stream);
            }
        }

        public void Load(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using (var reader = new StreamReader(stream))
            {
                Load(reader);
            }
        }

        public virtual void Load(TextReader reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            var serializer = CreateJsonSerializer();
            using (var jsonReader = new JsonTextReader(reader))
            {
                serializer.Populate(jsonReader, this);
                foreach (var htmlFile in Files)
                {
                    htmlFile.Project = this;
                    htmlFile.LoadHtml();

                    foreach (var field in htmlFile.Fields)
                    {
                        field.File = htmlFile;
                    }
                }
            }
        }

        protected virtual JsonSerializer CreateJsonSerializer()
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.ContractResolver = new ProjectContractResolver();
            serializer.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
            serializer.TypeNameHandling = TypeNameHandling.Auto;
            serializer.Formatting = Formatting.Indented;
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new StringListConverter());
            return serializer;
        }

        public virtual void OpenDirectory(DirectoryInfo di)
        {
            if (di == null) throw new ArgumentNullException(nameof(di));

            BaseDirectory = di;
            var files = EnumerateFiles(di);
            OpenDirectory(di, files);
        }

        public void OpenDirectory(DirectoryInfo root, IEnumerable<FileInfo> files)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (files == null) throw new ArgumentNullException(nameof(files));

            foreach (var file in files)
            {
                var filePath = GetFilePath(root, file);

                bool created = false;
                var htmlFile = FindFile(filePath);
                if (htmlFile == null)
                {
                    htmlFile = new HtmlFile();
                    created = true;
                }

                htmlFile.Project = this;
                htmlFile.Path = filePath;

                htmlFile.LoadHtml(File.ReadAllText(file.FullName));
                htmlFile.ExtractFields();

                // Do not add files without fields
                if (created && htmlFile.Fields.Any())
                {
                    Files.Add(htmlFile);
                }
            }
        }

        private HtmlFile FindFile(string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            return Files.FirstOrDefault(f => string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase));
        }

        protected virtual string GetFilePath(DirectoryInfo di, FileInfo fi)
        {
            if (di == null) throw new ArgumentNullException(nameof(di));
            if (fi == null) throw new ArgumentNullException(nameof(fi));

            return GetRelativePathTo(di, fi).Replace('\\', '/');
        }

        private static string GetRelativePathTo(FileSystemInfo from, FileSystemInfo to)
        {
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));

            Func<FileSystemInfo, string> getPath = fsi =>
            {
                var d = fsi as DirectoryInfo;
                return d == null ? fsi.FullName : d.FullName.TrimEnd('\\') + "\\";
            };

            var fromPath = getPath(from);
            var toPath = getPath(to);

            var fromUri = new Uri(fromPath);
            var toUri = new Uri(toPath);

            var relativeUri = fromUri.MakeRelativeUri(toUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        protected virtual IEnumerable<FileInfo> EnumerateFiles(DirectoryInfo di)
        {
            var files = new List<FileInfo>();
            EnumerateFiles(files, di);
            return files;
        }

        private void EnumerateFiles(ICollection<FileInfo> files, DirectoryInfo di)
        {
            if (di == null) throw new ArgumentNullException(nameof(di));

            try
            {
                var fileInfos = di.GetFiles("*.html");
                foreach (var fileInfo in fileInfos)
                {
                    files.Add(fileInfo);
                }
            }
            catch (SecurityException)
            {
            }
            catch (IOException)
            {
            }

            try
            {
                var directoryInfos = di.GetDirectories();
                foreach (var directoryInfo in directoryInfos)
                {
                    EnumerateFiles(files, directoryInfo);
                }
            }
            catch (SecurityException)
            {
            }
            catch (IOException)
            {
            }
        }

        public virtual void Localize(CultureInfo cultureInfo, FileLayout fileLayout)
        {
            foreach (var file in Files)
            {
                if (!file.CanLocalizeFile())
                    continue;

                var html = file.Localize(cultureInfo);
                string path = GenerateFileName(file, cultureInfo, fileLayout);
                if (BaseDirectory != null)
                {
                    path = Path.Combine(BaseDirectory.FullName, path);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, html);
            }
        }

        protected virtual string GenerateFileName(HtmlFile file, CultureInfo cultureInfo, FileLayout fileLayout)
        {
            var path = file.Path;
            if (string.IsNullOrEmpty(path))
                return path;

            switch (fileLayout)
            {
                case FileLayout.Extensions:
                    return Path.GetFileNameWithoutExtension(path) + "." + cultureInfo.Name + Path.GetExtension(path);
                case FileLayout.SubDirectory:
                    return Path.Combine(Path.GetDirectoryName(path), cultureInfo.Name, Path.GetFileName(path));
                default:
                    throw new ArgumentOutOfRangeException(nameof(fileLayout), fileLayout, null);
            }
        }
    }
}