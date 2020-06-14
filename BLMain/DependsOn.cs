using System;

namespace BLMain
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    sealed class DependsOnAttribute : Attribute
    {

        // This is a positional argument
        public DependsOnAttribute(Type dependency)
        {
            this.Dependency = dependency;
        }

        public Type Dependency { get; }
    }
}