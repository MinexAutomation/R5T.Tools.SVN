using System;


namespace R5T.Tools.SVN
{
    public abstract class SvnEntry<T>
    {
        public string Path { get; set; }
        public T Value { get; set; }
    }
}
