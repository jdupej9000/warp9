﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Warp9.Model
{
    public interface IProjectArchive : IDisposable
    {
        public bool IsOpen { get; }
        public string WorkingDirectory { get; }
        public bool ContainsFile(string name);
        public Stream OpenFile(string name);
        public Stream CreateFile(string name);
        public void CopyFileFrom(string name, IProjectArchive other);
        public void Close();
    }
}