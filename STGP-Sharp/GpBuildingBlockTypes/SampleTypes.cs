#nullable enable

#region

using System;
using System.Diagnostics;
using System.Globalization;
using STGP_Sharp.Utilities.GeneralCSharp;

#endregion

namespace STGP_Sharp.GpBuildingBlockTypes
{
    public abstract class BinaryOperator<TReturnType, TOperandType> : GpBuildingBlock<TReturnType>
    {
        protected BinaryOperator(GpBuildingBlock<TOperandType> left, GpBuildingBlock<TOperandType> right) :
            base(left, right)
        {
        }

        public GpBuildingBlock<TOperandType> Left => (GpBuildingBlock<TOperandType>)this.children[0];
        public GpBuildingBlock<TOperandType> Right => (GpBuildingBlock<TOperandType>)this.children[1];
    }

    public abstract class BooleanOperator : BinaryOperator<bool, bool>
    {
        protected BooleanOperator(GpBuildingBlock<bool> left, GpBuildingBlock<bool> right) :
            base(left, right)
        {
        }

        protected BooleanOperator() : base(new BooleanConstant(true), new BooleanConstant(true))
        {
        }
    }

    public class And : BooleanOperator
    {
        public And(GpBuildingBlock<bool> left, GpBuildingBlock<bool> right) :
            base(left, right)
        {
        }

        public And()
        {
        }


        public override bool Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            return this.Left.Evaluate(gpFieldsWrapper) && this.Right.Evaluate(gpFieldsWrapper);
        }
    }

    public class Not : GpBuildingBlock<bool>
    {
        public Not(GpBuildingBlock<bool> operand) : base(operand)
        {
        }

        public Not()
        {
        }

        public GpBuildingBlock<bool> Operand => (GpBuildingBlock<bool>)this.children[0];

        public override bool Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            return !this.Operand.Evaluate(gpFieldsWrapper);
        }
    }

    public class BooleanConstant : GpBuildingBlock<bool>
    {
        private readonly bool _value;

        public BooleanConstant(bool v)
        {
            this._value = v;
            this.symbol = v.ToString(CultureInfo.InvariantCulture);
        }

        [RandomTreeConstructor]
        public BooleanConstant(GpFieldsWrapper gpFieldsWrapper) : base(gpFieldsWrapper)
        {
            this._value = gpFieldsWrapper.rand.NextBool();
            this.symbol = this._value.ToString(CultureInfo.InvariantCulture);
        }

        public override bool Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            return this._value;
        }
    }

    public class PositionalArgument<TArgumentType> : GpBuildingBlock<TArgumentType>
    {
        public int argIndex;

        [RandomTreeConstructor]
        public PositionalArgument(GpFieldsWrapper gpFieldsWrapper) : base(gpFieldsWrapper)
        {
            if (null == gpFieldsWrapper.positionalArguments)
            {
                throw new Exception("Positional arguments is null");
            }

            this.argIndex = gpFieldsWrapper.positionalArguments.PopNextIndex();
        }

        public PositionalArgument(int argIndex)
        {
            this.argIndex = argIndex;
        }

        public override TArgumentType Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            if (null == gpFieldsWrapper.positionalArguments)
            {
                throw new Exception("Positional arguments is null");
            }

            if (gpFieldsWrapper.positionalArguments.MapToTypedArgument(this.argIndex, out TArgumentType? arg))
            {
                Debug.Assert(null != arg);

                return arg;
            }

            throw new Exception($"Positional argument at index {this.argIndex} is not of type {typeof(TArgumentType)}");
        }
    }


    public class BooleanPositionalArgument : PositionalArgument<bool>
    {
        [RandomTreeConstructor]
        public BooleanPositionalArgument(GpFieldsWrapper gpFieldsWrapper) : base(gpFieldsWrapper)
        {
            this.symbol = $"BoolArg{this.argIndex}";
        }

        public BooleanPositionalArgument(int argIndex) : base(argIndex)
        {
            this.symbol = $"BoolArg{this.argIndex}";
        }
    }
}