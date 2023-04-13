#nullable enable

#region

using System.Collections.Generic;
using System.Linq;
using STGP_Sharp.Utilities.GeneralCSharp;

#endregion

namespace STGP_Sharp
{
    public class PositionalArguments
    {
        private readonly MultiTypeCollection _positionalArguments;
        private int _positionalArgumentsUsed;

        public PositionalArguments(MultiTypeCollection args)
        {
            this._positionalArguments = args;
        }

        public PositionalArguments()
        {
            this._positionalArguments = new MultiTypeCollection();
        }

        public PositionalArguments(params object[] args)
        {
            this._positionalArguments = new MultiTypeCollection();
            foreach (object arg in args)
            {
                this._positionalArguments.Add(arg.GetType(), arg);
            }
        }

        public int PopNextIndex()
        {
            return this._positionalArgumentsUsed++;
        }

        public bool MapToTypedArgument<T>(int untypedIndex, out T? arg)
        {
            int typedIndex = this.GetTypedIndex<T>(untypedIndex, out int numOfType);
            if (numOfType < 1)
            {
                arg = default;
                return false;
            }

            arg = this._positionalArguments.Get<T>().Skip(typedIndex).FirstOrDefault();
            return true;
        }

        public static implicit operator PositionalArguments(object[] args)
        {
            return new PositionalArguments(args);
        }

        public static implicit operator PositionalArguments(List<object> args)
        {
            return new PositionalArguments(args.ToArray());
        }

        public int GetTypedIndex<T>(int untypedIndex, out int numOfType)
        {
            numOfType = this._positionalArguments.Get<T>().Count();
            int typedIndex = untypedIndex % numOfType;
            return typedIndex;
        }
    }
}