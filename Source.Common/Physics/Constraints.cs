using CommunityToolkit.HighPerformance;

using Source.Common.Engine;
using Source.Common.Mathematics;
using Source.Common.Physics;

using System.Numerics;

namespace Source.Common.Physics;

public interface IPhysicsConstraint
{

}

public interface IPhysicsConstraintGroup
{

}

public struct ConstraintGroupParams
{
	int additionalIterations;       // additional solver iterations make the constraint system more stable
	int minErrorTicks;              // minimum number of ticks with an error before it's reported
	float errorTolerance;           // error tolerance in HL units

	public void Defaults() {
		additionalIterations = 0;
		minErrorTicks = 15;
		errorTolerance = 3.0f;
	}
}

public struct ConstraintBreakableParams
{
	public float Strength;                      // strength of the constraint 0.0 - 1.0
	public float ForceLimit;                    // constraint force limit to break (0 means never break)
	public float TorqueLimit;                   // constraint torque limit to break (0 means never break)
	public InlineArray2<float> BodyMassScale;   // scale applied to mass of reference/attached object before solving constriant
	public bool IsActive;

	public void Defaults() {
		ForceLimit = 0.0f;
		TorqueLimit = 0.0f;
		Strength = 1.0f;
		BodyMassScale[0] = 1.0f;
		BodyMassScale[1] = 1.0f;
		IsActive = true;
	}
}

public struct ConstraintAxisLimit
{
	public float MinRotation;
	public float MaxRotation;
	public float AngularVelocity;    // desired angular velocity around hinge
	public float Torque;             // torque to achieve angular velocity (use 0, torque for "friction")

	public void SetAxisFriction(float rmin, float rmax, float friction) {
		MinRotation = rmin;
		MaxRotation = rmax;
		AngularVelocity = 0;
		Torque = friction;
	}
	public void Defaults() {
		SetAxisFriction(0, 0, 0);
	}
}

public static class ConstraintFunctions
{

	public static void BuildObjectRelativeXform(IPhysicsObject outputSpace, IPhysicsObject inputSpace, out Matrix3x4 xformInToOut) {
		Matrix3x4 outInv, tmp, input;
		outputSpace.GetPositionMatrix(out tmp);
		MathLib.MatrixInvert(tmp, out outInv);
		inputSpace.GetPositionMatrix(out input);
		MathLib.ConcatTransforms(outInv, input, out xformInToOut);
	}

}

public struct ConstraintRagdollParams
{
	public ConstraintBreakableParams Constraint;
	public Matrix3x4 ConstraintToReference;// xform constraint space to refobject space
	public Matrix3x4 ConstraintToAttached;   // xform constraint space to attached object space
	public int ParentIndex;                // NOTE: only used for parsing.  NEED NOT BE SET for create
	public int ChildIndex;                 // NOTE: only used for parsing.  NEED NOT BE SET for create

	public InlineArray3<ConstraintAxisLimit> Axes;
	public bool OnlyAngularLimits;         // only angular limits (not translation as well?)
	public bool IsActive;
	public bool UseClockwiseRotations;     // HACKHACK: Did this wrong in version one.  Fix in the future.

	public void Defaults() {
		Constraint.Defaults();
		IsActive = true;
		MathLib.SetIdentityMatrix(out ConstraintToReference);
		MathLib.SetIdentityMatrix(out ConstraintToAttached);
		ParentIndex = -1;
		ChildIndex = -1;
		Axes[0].Defaults();
		Axes[1].Defaults();
		Axes[2].Defaults();
		OnlyAngularLimits = false;
		UseClockwiseRotations = false;
	}
}

public struct ConstraintHingeParams
{
	Vector3 WorldPosition;           // position in world space on the hinge axis
	Vector3 WorldAxisDirection;      // unit direction vector of the hinge axis in world space
	ConstraintAxisLimit HingeAxis;
	ConstraintBreakableParams Constraint;

	public void Defaults() {
		WorldPosition.Init();
		WorldAxisDirection.Init();
		HingeAxis.Defaults();
		Constraint.Defaults();
	}
}

public struct ConstraintLimitedHingeParams
{
	public ConstraintHingeParams Hinge;
	public Vector3 ReferencePerpAxisDirection;      // unit direction vector vector perpendicular to the hinge axis in world space
	public Vector3 AttachedPerpAxisDirection;       // unit direction vector vector perpendicular to the hinge axis in world space
}

public struct ConstraintFixedParams
{
	Matrix3x4 AttachedRefXform;   // xform attached object space to ref object space
	ConstraintBreakableParams Constraint;

	public void InitWithCurrentObjectState(IPhysicsObject refObj, IPhysicsObject attached) {
		ConstraintFunctions.BuildObjectRelativeXform(refObj, attached, out AttachedRefXform);
	}

	public void Defaults() {
		MathLib.SetIdentityMatrix(out AttachedRefXform);
		Constraint.Defaults();
	}
}

public struct ConstraintBallSocketParams
{
	InlineArray2<Vector3> ConstraintPosition;       // position of the constraint in each object's space 
	ConstraintBreakableParams Constraint;
	public void Defaults() {
		Constraint.Defaults();
		ConstraintPosition[0].Init();
		ConstraintPosition[1].Init();
	}

	public void InitWithCurrentObjectState(IPhysicsObject refObj, IPhysicsObject attached, in Vector3 ballsocketOrigin) {
		refObj.WorldToLocal(out ConstraintPosition[0], ballsocketOrigin);
		attached.WorldToLocal(out ConstraintPosition[1], ballsocketOrigin);
	}
}

public struct ConstraintSlidingParams
{
	public Matrix3x4 AttachedRefXform;   // xform attached object space to ref object space
	public Vector3 SlideAxisRef;                // unit direction vector of the slide axis in ref object space
	public ConstraintBreakableParams Constraint;
	// NOTE: if limitMin == limitMax there is NO limit set!
	public float LimitMin;             // minimum limit coordinate refAxisDirection space
	public float LimitMax;             // maximum limit coordinate refAxisDirection space
	public float Friction;             // friction on sliding
	public float Velocity;             // desired velocity

	public void Defaults() {
		MathLib.SetIdentityMatrix(out AttachedRefXform);
		SlideAxisRef.Init();
		LimitMin = LimitMax = 0;
		Friction = 0;
		Velocity = 0;
		Constraint.Defaults();
	}

	public void SetFriction(float inputFriction) {
		Friction = inputFriction;
		Velocity = 0;
	}

	public void SetLinearMotor(float inputVelocity, float maxForce) {
		Friction = maxForce;
		Velocity = inputVelocity;
	}

	public void InitWithCurrentObjectState(IPhysicsObject refObj, IPhysicsObject attached, in Vector3 slideDirWorldspace) {
		ConstraintFunctions.BuildObjectRelativeXform(refObj, attached, out AttachedRefXform);
		Matrix3x4 tmp;
		refObj.GetPositionMatrix(out tmp);
		MathLib.VectorIRotate(slideDirWorldspace, tmp, out SlideAxisRef);
	}
}

public struct ConstraintPulleyParams
{
	public ConstraintBreakableParams Constraint;
	public InlineArray2<Vector3> PulleyPosition;       // These are the pulley positions for the reference and attached objects in world space
	public InlineArray2<Vector3> ObjectPosition;       // local positions of attachments to the ref,att objects
	public float TotalLength;          // total rope length (include gearing!)
	public float GearRatio;                // gearing affects attached object ALWAYS
	public bool IsRigid;

	public void Defaults() {
		Constraint.Defaults();
		TotalLength = 1.0f;
		GearRatio = 1.0f;
		PulleyPosition[0].Init();
		PulleyPosition[1].Init();
		ObjectPosition[0].Init();
		ObjectPosition[1].Init();
		IsRigid = false;
	}
}

public struct ConstraintLengthParams
{
	public ConstraintBreakableParams Constraint;
	public InlineArray2<Vector3> ObjectPosition;       // These are the positions for the reference and attached objects in local space
	public float TotalLength;      // Length of rope/spring/constraint.  Distance to maintain
	public float MinLength;            // if rigid, objects are not allowed to move closer than totalLength either

	void InitWorldspace(IPhysicsObject refObj, IPhysicsObject attached, in Vector3 refPosition, in Vector3 attachedPosition, bool rigid = false) {
		refObj.WorldToLocal(out ObjectPosition[0], refPosition);
		attached.WorldToLocal(out ObjectPosition[1], attachedPosition);
		TotalLength = (refPosition - attachedPosition).Length();
		MinLength = rigid ? TotalLength : 0;
	}

	public void Defaults() {
		Constraint.Defaults();
		ObjectPosition[0].Init();
		ObjectPosition[1].Init();
		TotalLength = 1;
		MinLength = 0;
	}
}
