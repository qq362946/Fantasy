// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8603 // Possible null reference return.
namespace Fantasy
{
    public struct EntityReference<T> where T : Entity
    {
        private T _entity;
        private readonly long _runTimeId;

        private EntityReference(T t)
        {
            if (t == null)
            {
                _entity = null;
                _runTimeId = 0;
                return;
            }
            
            _entity = t;
            _runTimeId = t.RunTimeId;
        }

        public static implicit operator EntityReference<T>(T t)
        {
            return new EntityReference<T>(t);
        }

        public static implicit operator T(EntityReference<T> v)
        {
            if (v._entity == null)
            {
                return null;
            }

            if (v._entity.RunTimeId != v._runTimeId)
            {
                v._entity = null;
            }

            return v._entity;
        }
    }
}