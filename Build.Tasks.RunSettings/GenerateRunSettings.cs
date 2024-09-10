using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.IO;
using System.Xml;
using System.Linq;
using System.Collections.Generic;

namespace Linkoid.Build.Tasks.RunSettings;

public class GenerateRunSettings : Task
{
    [Required]
    public ITaskItem RunSettingsFile { get; set; } = default!;

    public ITaskItem[] Elements { get; set; } = [];
    public ITaskItem[] Attributes { get; set; } = [];

    public override bool Execute()
    {
        XmlDocument doc = new()
        {
            PreserveWhitespace = true,
        };

        if (File.Exists(RunSettingsFile.ItemSpec))
        {
            doc.Load(RunSettingsFile.ItemSpec);
        }
        else
        {
            BuildEmptyRunSettings(doc);
        }

        foreach (var item in Elements)
        {
            var element = GetOrCreateElementAtPath(doc, item.ItemSpec);
            SetTargetElements(doc, element, item);
        }

        foreach (var item in Attributes)
        {
            var element = GetOrCreateElementAtPath(doc, item.ItemSpec);
            SetTargetAttributes(doc, element, item);
        }

        using XmlWriter writer = XmlWriter.Create(RunSettingsFile.ItemSpec, new()
        {
            Encoding = System.Text.Encoding.UTF8,
            Indent = true,
            IndentChars = "  ",
            NewLineHandling = NewLineHandling.Replace,
        });
        doc.Save(writer);

        return true;
    }

    private void BuildEmptyRunSettings(XmlDocument doc)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));

        doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
        doc.AppendChild(doc.CreateComment("This file was automatically generated. " +
            "Additions are fine, but some changes may be overwritten on build."));
        doc.AppendChild(doc.CreateElement("RunSettings"));
    }

    private XmlNamespaceManager BuildRunSettingsNamespaceManager(XmlDocument doc)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));

        XmlNamespaceManager namespaceManager = new(doc.NameTable);
        return namespaceManager;
    }

    private XmlElement GetOrCreateElementAtPath(XmlDocument doc, string path, XmlElement? parent = null)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

        XmlElement currentElement = parent ?? doc.DocumentElement
            ?? throw new ArgumentNullException(nameof(parent));

        string[] parts = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);
        if (path.StartsWith("/") && parts.Length > 0 )
        {
            if (!parts[0].Equals("RunSettings"))
            {
                throw new InvalidOperationException($"The root element must be '/RunSettings', but instead was '/{parts[0]}'. " +
                    $"(Omit the leading slash for a path relative to '/RunSettings'.)");
            }
            parts = parts.Skip(1).ToArray();
        }

        foreach (var part in parts)
        {
            var nextElement = GetOrCreateChildElement(doc, currentElement, part);
            currentElement = nextElement;
        }
        return currentElement;
    }

    private XmlElement GetOrCreateChildElement(XmlDocument doc, XmlNode parent, string name)
    {
        if (parent.HasChildNodes)
        {
            foreach (var child in parent.ChildNodes)
            {
                if (child is not XmlElement childElement) continue;
                if (childElement.Name.Equals(name, StringComparison.InvariantCulture))
                {
                    return childElement;
                }
            }
        }
        return (XmlElement)parent.AppendChild(doc.CreateElement(name))!;
    }

    private IEnumerable<KeyValuePair<string, string>> GetMetadataItems(ITaskItem taskItem, bool includeBuiltIn = false)
    {
        if (taskItem == null) throw new ArgumentNullException(nameof(taskItem));

        string[] metadataNames = taskItem.MetadataNames.Cast<string>().ToArray();
        for (int i = 0; i < taskItem.MetadataCount; i++)
        {
            var key = metadataNames[i];
            switch (key)
            {
                default:
                    var value = taskItem.GetMetadata(key);
                    yield return new KeyValuePair<string, string>(key, value);
                    break;

                case "FullPath":
                case "RootDir":
                case "Filename":
                case "Extension":
                case "RelativeDir":
                case "Directory":
                case "RecursiveDir":
                case "Identity":
                case "ModifiedTime":
                case "CreatedTime":
                case "AccessedTime":
                case "DefiningProjectFullPath":
                case "DefiningProjectDirectory":
                case "DefiningProjectName":
                case "DefiningProjectExtension":
                    if (includeBuiltIn) goto default;
                    break;
            }
        }
    }

    private void SetTargetElements(XmlDocument doc, XmlElement target, ITaskItem taskItem)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (taskItem == null) throw new ArgumentNullException(nameof(taskItem));

        foreach (var pair in GetMetadataItems(taskItem))
        {
            string key = pair.Key.StartsWith("_") ? pair.Key.Substring(1) : pair.Key;
            GetOrCreateChildElement(doc, target, key).InnerText = pair.Value;
        }
    }

    private void SetTargetAttributes(XmlDocument doc, XmlElement target, ITaskItem taskItem)
    {
        if (doc == null) throw new ArgumentNullException(nameof(doc));
        if (target == null) throw new ArgumentNullException(nameof(target));
        if (taskItem == null) throw new ArgumentNullException(nameof(taskItem));

        foreach (var pair in GetMetadataItems(taskItem))
        {
            switch (pair.Key)
            {
                case nameof(XmlElement.InnerText):
                    target.InnerText = pair.Value;
                    break;

                case nameof(XmlElement.InnerXml):
                    target.InnerXml = pair.Value;
                    break;

                default:
                    string key = pair.Key.StartsWith("_") ? pair.Key.Substring(1) : pair.Key;
                    target.SetAttribute(key, pair.Value);
                    break;
            }
        }
    }
}


