using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace WPSC.WcData
{
    public class Category
    {
        public Category(WCMap map, WtgCategory inner)
        {
            _map = map;
            Inner = inner;
        }

        public void Remove() => _map.Remove(Inner.Id);
        public Category CreateCategory(string name) => _map.CreateCategory(name, Inner.Id);
        public Script CreateScript(string name) => _map.CreateScript(name, Inner.Id);

        internal WtgCategory Inner { get; private set; }

        private WCMap _map;
    }

    public class Script
    {
        public Script(WCMap map, WtgTrigger inner, int source)
        {
            _map = map;
            Inner = inner;
            _source = source;
        }

        public string Source
        {
            get => _map.GetSource(_source);
            set => _map.SetSource(_source, value);
        }

        internal WtgTrigger Inner { get; private set; }

        private WCMap _map;
        private int _source;
    }

    public class WCMap
    {
        public WCMap(string path)
        {
            _path = path;
            using (var wct = File.OpenRead(Path.Combine(path, "war3map.wct")))
                _wct = new WCT(wct);
            using (var wtg = File.OpenRead(Path.Combine(path, "war3map.wtg")))
                _wtg = new WTG(wtg);
            using (var lua = new StreamReader(File.OpenRead(Path.Combine(path, "war3map.lua"))))
                _scriptLeftovers = lua.ReadToEnd();
            foreach (var scriptText in _wct.Triggers)
                _scriptLeftovers = _scriptLeftovers.Replace($"{scriptText}\r\n", "");
        }

        public void Save(string? overridePath = null)
        {
            using (var wct = File.Create(Path.Combine(overridePath ?? _path, "war3map.wct")))
                _wct.Save(wct);
            using (var wtg = File.Create(Path.Combine(overridePath ?? _path, "war3map.wtg")))
                _wtg.Save(wtg);

            using (var lua = File.Create(Path.Combine(overridePath ?? _path, "war3map.lua")))
            using (var writer = new StreamWriter(lua, Encoding.UTF8, 1024, true))
            {
                var globalsStart = _scriptLeftovers.IndexOf("function InitGlobals()");
                var globalsEnd = _scriptLeftovers.IndexOf("end", globalsStart);

                writer.WriteLine(_scriptLeftovers[0..(globalsEnd + 3)].TrimEnd());
                writer.WriteLine();
                foreach (var scriptText in _wct.Triggers)
                    writer.WriteLine(scriptText);
                writer.Write(_scriptLeftovers[(globalsEnd + 4)..^1].TrimStart());
            }
        }

        public Category? FindCategory(string name)
        {
            var inner = _wtg.Categories.FirstOrDefault(cat => cat!.Name == name);
            return inner != null ? new Category(this, inner) : null;
        }

        public Category CreateCategory(string name)
        {
            var inner = new WtgCategory
            {
                Name = name,
                HasChildren = false,
                IsComment = false,
                Id = (_wtg.Categories.Max(c => c?.Id) ?? 0x200_0000) + 1
            };

            _wtg.Elements.Add(inner);

            return new Category(this, inner);
        }

        private string _path;
        private WCT _wct;
        private WTG _wtg;
        private string _scriptLeftovers;

        internal string GetSource(int index) => _wct.Triggers[index];
        internal void SetSource(int index, string value) => _wct.Triggers[index] = value;

        internal Category CreateCategory(string name, uint parent)
        {
            var ret = CreateCategory(name);
            ret.Inner.ParentId = parent;
            AddToParent(parent);
            return ret;
        }

        internal Script CreateScript(string name, uint parent)
        {
            var inner = new WtgTrigger
            {
                Name = name,
                IsComment = false,
                IsEnabled = true,
                IsEnabledFromStart = true,
                InitializeOnMapStart = false,
                IsText = true,
                Id = (_wtg.CustomScripts.Max(c => c?.Id) ?? 0x500_0000) + 1,
                ParentId = parent,
            };

            _wtg.Elements.Add(inner);
            AddToParent(parent);
            var source = _wct.Triggers.Count;
            _wct.Triggers.Add("");

            return new Script(this, inner, source);
        }

        internal WtgObject Remove(uint id)
        {
            var index = _wtg.Elements.FindIndex(o => o.Id == id);
            var ret = _wtg.Elements[index];

            if (ret is WtgTrigger trig && trig.IsText)
            {
                var scriptIndex = _wtg.CustomScripts.TakeWhile(t => t.Id != id).Count();
                _wct.Triggers.RemoveAt(scriptIndex);
            }

            _wtg.Elements.RemoveAt(index);

            if (ret is WtgCategory _)
                RemoveChildren(id);

            var pid = ret.ParentId;
            if (pid != 0 && (_wtg.Elements.All(obj => obj.ParentId != pid)))
            {
                var parent = _wtg.Categories.FirstOrDefault(c => c.Id == pid);
                if (parent != null)
                    parent.HasChildren = false;
            }

            return ret;
        }

        private void RemoveChildren(uint parent)
        {
            var element = _wtg.Elements.FirstOrDefault(o => o.ParentId == parent);
            while (element != null)
            {
                Remove(element.Id);
                element = _wtg.Elements.FirstOrDefault(o => o.ParentId == parent);
            }
        }

        private void AddToParent(uint parent) => _wtg.Categories.First(c => c?.Id == parent)!.HasChildren = true;
    }
}
