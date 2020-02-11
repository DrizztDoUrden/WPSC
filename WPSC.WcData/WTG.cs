using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WPSC.WcData
{
    public class WtgObject
    {
        public uint Id { get; set; }
        public string Name { get; set; } = "";
        public uint ParentId { get; set; }
    }

    public class WtgCategory : WtgObject
    {
        public bool IsComment { get; set; } = false;
        public bool HasChildren { get; set; } = false;
    }

    public class WtgTrigger : WtgObject
    {
        public string Description { get; set; } = "";
        public bool IsComment { get; set; } = false;
        public bool IsEnabled { get; set; } = true;
        public bool IsText { get; set; } = false;
        public bool IsEnabledFromStart { get; set; } = true;
        public bool InitializeOnMapStart { get; set; } = false;
        public uint EACCount { get; set; } = 0;
    }

    public class WtgVariable : WtgObject
    {
        public string Type { get; set; } = "";
        public bool ToKeep { get; set; } = true;
        public bool IsArray { get; set; } = false;
        public uint ArraySize { get; set; }
        public bool HasStartingValue { get; set; } = false;
        public string StartingValue { get; set; } = "";

        public bool StoreAsElement { get; set; } = false;
    }

    public class WTG
    {
        public const string MagicNumber0 = "WTG!";
        public const uint MagicNumber1 = 0x80000004;

        public uint FormatVersion { get; set; }
        public uint[] UG0 { get; } = new uint[4];
        public List<WtgObject> Elements = new List<WtgObject>();
        public IEnumerable<WtgCategory> Categories => Elements.Where(e => e is WtgCategory).Cast<WtgCategory>();
        public IEnumerable<WtgTrigger> Triggers => Elements.Where(e => e is WtgTrigger).Cast<WtgTrigger>().Where(t => !t.IsText && !t.IsComment);
        public IEnumerable<WtgTrigger> TriggerComments => Elements.Where(e => e is WtgTrigger).Cast<WtgTrigger>().Where(t => t.IsComment);
        public IEnumerable<WtgTrigger> CustomScripts => Elements.Where(e => e is WtgTrigger).Cast<WtgTrigger>().Where(t => t.IsText);
        public IEnumerable<WtgVariable> Variables => Elements.Where(e => e is WtgVariable).Cast<WtgVariable>();
        public uint[] UG1 { get; } = new uint[2];
        public uint WarcraftVersion { get; set; }
        public uint[] UG2 { get; } = new uint[2];
        public string MapFileName { get; set; }
        public uint[] UG3 { get; } = new uint[2];
        public uint[] UG4 { get; } = new uint[1];

        public WTG(Stream from)
        {
            var reader = new WcDataReader(from);
            var mg0 = Encoding.ASCII.GetString(reader.ReadBytes(4));

            if (mg0 != MagicNumber0)
                throw new Exception("Invalid wtg magic string.");
            if (reader.ReadUInt32() != MagicNumber1)
                throw new Exception("Invalid wtg magic number.");

            FormatVersion = reader.ReadUInt32();
            if (FormatVersion != 4 && FormatVersion != 7)
                throw new Exception("Unexpected format version of wct file.");

            ReadUG(reader, UG0);

            IgnoreHeader(reader);
            IgnoreHeader(reader);
            IgnoreHeader(reader);
            IgnoreHeader(reader);
            IgnoreHeader(reader);

            ReadUG(reader, UG1);

            WarcraftVersion = reader.ReadUInt32();
            if (WarcraftVersion != 1 && WarcraftVersion != 2)
                throw new Exception("Unexpected warcraft version of wct file.");

            var varsCount = reader.ReadUInt32();
            for (var i = 0; i < varsCount; ++i)
                Elements.Add(ParseVariable(reader));

            var elementsCount = reader.ReadUInt32();
            Elements = new List<WtgObject>((int)elementsCount);
            ReadUG(reader, UG2);
            MapFileName = reader.ReadString();
            ReadUG(reader, UG3);

            if (FormatVersion >= 7)
                ReadUG(reader, UG4);

            for (var i = 0u; i < (int)elementsCount - 1 && from.Position < from.Length; ++i)
            {
                var type = reader.ReadUInt32();
                switch (type)
                {
                    case 4: Elements.Add(ParseCategory(reader)); break;
                    case 8: Elements.Add(ParseTrigger(reader)); break;
                    case 16: Elements.Add(ParseTrigger(reader)); break;
                    case 32: Elements.Add(ParseTrigger(reader)); break;
                    case 64: ParseVariable2(reader, Variables.First(v => v.Id == reader.ReadUInt32())!); break;
                }
            }
        }

        public void Save(Stream to)
        {
            using var writer = new WcDataWriter(to);
            writer.Write(Encoding.ASCII.GetBytes(MagicNumber0));
            writer.Write(MagicNumber1);
            writer.Write(FormatVersion);
            foreach (var uv in UG0) writer.Write(uv);

            writer.Write(Categories.Count() + 1);
            writer.Write(0u);
            writer.Write(Triggers.Count());
            writer.Write(0u);
            writer.Write(TriggerComments.Count());
            writer.Write(0u);
            writer.Write(CustomScripts.Count());
            writer.Write(0u);
            writer.Write(Variables.Count());
            writer.Write(0u);

            foreach (var uv in UG1) writer.Write(uv);
            writer.Write(WarcraftVersion);
            writer.Write(Variables.Count());
            foreach (var v in Variables)
                Produce(writer, v!);
            writer.Write(Elements.Count + 1);
            foreach (var uv in UG2) writer.Write(uv);
            writer.Write(MapFileName);
            foreach (var uv in UG3) writer.Write(uv);
            if (FormatVersion >= 7)
                foreach (var uv in UG4) writer.Write(uv);

            foreach (var obj in Elements)
            {
                switch (obj)
                {
                    case WtgCategory cat: Produce(writer, cat); break;
                    case WtgTrigger trig: Produce(writer, trig); break;
                    case WtgVariable v: Produce2(writer, v); break;
                }
            }
        }

        private void Produce(WcDataWriter writer, WtgVariable variable)
        {
            writer.Write(variable.Name);
            writer.Write(variable.Type);
            writer.Write(variable.ToKeep);
            writer.Write(variable.IsArray);
            if (FormatVersion >= 7)
                writer.Write(variable.ArraySize);
            writer.Write(variable.HasStartingValue);
            writer.Write(variable.Id);
            writer.Write(variable.ParentId);
        }

        private void Produce(WcDataWriter writer, WtgCategory category)
        {
            writer.Write(4u);
            writer.Write(category.Id);
            writer.Write(category.Name);
            if (FormatVersion >= 7)
                writer.Write(category.IsComment);
            writer.Write(category.HasChildren);
            writer.Write(category.ParentId);
        }

        private void Produce(WcDataWriter writer, WtgTrigger trigger)
        {
            if (trigger.IsComment)
                writer.Write(16u);
            else if (trigger.IsText)
                writer.Write(32u);
            else
                writer.Write(8u);
            writer.Write(trigger.Name);
            writer.Write(trigger.Description);
            if (FormatVersion >= 7)
                writer.Write(trigger.IsComment);
            writer.Write(trigger.Id);
            writer.Write(trigger.IsEnabled);
            writer.Write(trigger.IsText);
            writer.Write(trigger.IsEnabledFromStart);
            writer.Write(trigger.InitializeOnMapStart);
            writer.Write(trigger.ParentId);
            writer.Write(trigger.EACCount);

            if (trigger.EACCount > 0)
                throw new NotImplementedException("Maps with ANY GUI triggers are not supported yet.");
        }

        private void Produce2(WcDataWriter writer, WtgVariable variable)
        {
            if (!variable.StoreAsElement)
                return;
            writer.Write(64u);
            writer.Write(variable.Id);
            writer.Write(variable.Name);
            writer.Write(variable.ParentId);
        }

        private void ReadUG(WcDataReader reader, uint[] ug)
        {
            for (var i = 0u; i < ug.Length; ++i)
                ug[i] = reader.ReadUInt32();
        }

        private void IgnoreHeader(WcDataReader reader)
        {
            var count = reader.ReadUInt32();
            var deleted = reader.ReadUInt32();
            for (var i = 0u; i < deleted; ++i)
                reader.ReadUInt32();
        }

        private WtgVariable ParseVariable(WcDataReader reader)
        {
            var variable = new WtgVariable();
            variable.Name = reader.ReadString();
            variable.Type = reader.ReadString();
            variable.ToKeep = reader.ReadBoolean();
            variable.IsArray = reader.ReadBoolean();
            if (FormatVersion >= 7)
                variable.ArraySize = reader.ReadUInt32();
            variable.HasStartingValue = reader.ReadBoolean();
            variable.StartingValue = reader.ReadString();
            variable.Id = reader.ReadUInt32();
            variable.ParentId = reader.ReadUInt32();
            return variable;
        }

        private WtgCategory ParseCategory(WcDataReader reader)
        {
            var category = new WtgCategory();
            category.Id = reader.ReadUInt32();
            category.Name = reader.ReadString();
            if (FormatVersion >= 7)
                category.IsComment = reader.ReadBoolean();
            category.HasChildren = reader.ReadBoolean();
            category.ParentId = reader.ReadUInt32();
            return category;
        }

        private WtgTrigger ParseTrigger(WcDataReader reader)
        {
            var trigger = new WtgTrigger();
            trigger.Name = reader.ReadString();
            trigger.Description = reader.ReadString();
            if (FormatVersion >= 7)
                trigger.IsComment = reader.ReadBoolean();
            trigger.Id = reader.ReadUInt32();
            trigger.IsEnabled = reader.ReadBoolean();
            trigger.IsText = reader.ReadBoolean();
            trigger.IsEnabledFromStart = reader.ReadBoolean();
            trigger.InitializeOnMapStart = reader.ReadBoolean();
            trigger.ParentId = reader.ReadUInt32();
            trigger.EACCount = reader.ReadUInt32();

            if (trigger.EACCount > 0)
                throw new NotImplementedException("Maps with ANY GUI triggers not supported yet.");

            return trigger;
        }

        private void ParseVariable2(WcDataReader reader, WtgVariable variable)
        {
            if (reader.ReadString() != variable.Name)
                throw new Exception("Variable data mismatch.");
            if (reader.ReadUInt32() != variable.ParentId)
                throw new Exception("Variable data mismatch.");
            variable.StoreAsElement = true;
        }
    }
}
