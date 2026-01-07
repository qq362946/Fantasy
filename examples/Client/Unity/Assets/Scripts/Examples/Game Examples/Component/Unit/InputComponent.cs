using Fantasy.Entitas;
using Fantasy.Entitas.Interface;
using Fantasy.Event;
using UnityEngine;

namespace Fantasy
{
    public sealed class InputComponentAwakeSystem : AwakeSystem<InputComponent>
    {
        protected override void Awake(InputComponent self)
        {
            self.Initialize();
        }
    }
    
    public sealed class InputComponentUpdateSystem : UpdateSystem<InputComponent>
    {
        protected override void Update(InputComponent self)
        {
            self.Update();
        }
    }

    public sealed class InputComponent : Entity
    {
        private Unit _unit;
        private EventComponent  _eventComponent;

        public void Initialize()
        {
            _unit = GetParent<Unit>();
            _eventComponent = Scene.EventComponent;
        }

        public override void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            _unit = null;
            _eventComponent = null;
            base.Dispose();
        }

        public void Update()
        {
            HandleMouseInput();
        }

        private void HandleMouseInput()
        {
            if (Camera.main == null || !Input.GetMouseButtonDown(0))
            {
                return;
            }

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit))
            {
                var hitInfoPoint = hit.point;
                _eventComponent.Publish(new OnInputMovePosition(_unit, ref hitInfoPoint));
            }
        }
    }
}