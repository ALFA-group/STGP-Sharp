using System.Globalization;
using System.Numerics;
using STGP_Sharp.GpBuildingBlockTypes;
using STGP_Sharp.Utilities.GeneralCSharp;

namespace STGP_Sharp.STGP_Sharp.Tests
{
    public class Vector2Constant : GpBuildingBlock<Vector2>
    {
        private readonly Vector2 _vector2Representation;

        public Vector2Constant(float x, float y)
        {
            this._vector2Representation = new Vector2(x, y);
            this.symbol = this._vector2Representation.ToString();
        }

        [RandomTreeConstructor]
        public Vector2Constant(GpFieldsWrapper gpFieldsWrapper) : base(gpFieldsWrapper)
        {
            this._vector2Representation =
                gpFieldsWrapper.rand.NextVector2();
            this.symbol = this._vector2Representation.ToString();
        }
        
        public override Vector2 Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            return this._vector2Representation;
        }
    }
    
    
    public class FloatConstant : GpBuildingBlock<float>
    {
        protected float value; 

        public FloatConstant(float v)
        {
            this.value = v;
            this.symbol = v.ToString(CultureInfo.InvariantCulture);
        }

        [RandomTreeConstructor]
        public FloatConstant(GpFieldsWrapper gpFieldsWrapper) : base(gpFieldsWrapper)
        {
            this.value = gpFieldsWrapper.rand.NextFloat();
            this.symbol = this.value.ToString(CultureInfo.InvariantCulture);
        }

        public override float Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            return this.value;
        }
    }


    public class DiscreteFloatConstant : FloatConstant
    {
        public DiscreteFloatConstant(float value) : base(value)
        {
        }

        [RandomTreeConstructor]
        public DiscreteFloatConstant(GpFieldsWrapper gpFieldsWrapper) : base(gpFieldsWrapper)
        {
            this.value = this.value.Quantize(
                4, 0, 1);
            this.symbol = this.value.ToString(CultureInfo.InvariantCulture);
        }
    }

    public class TwoVectorsOneFloat : GpBuildingBlock<Vector2>
    {
        public TwoVectorsOneFloat(
            GpBuildingBlock<Vector2> target,
            GpBuildingBlock<Vector2> target2,
            GpBuildingBlock<float> closeEnough) : base(target, target2, closeEnough)
        {
        }

        public GpBuildingBlock<Vector2> Vector2_1 => (GpBuildingBlock<Vector2>)this.children[0];

        public GpBuildingBlock<Vector2> Vector2_2 => (GpBuildingBlock<Vector2>)this.children[1];

        public GpBuildingBlock<float> Float => (GpBuildingBlock<float>)this.children[2];


        public override Vector2 Evaluate(GpFieldsWrapper gpFieldsWrapper)
        {
            return Vector2.Zero;
        }
    }
}