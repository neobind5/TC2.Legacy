namespace TC2.Base.Components
{
    public static partial class Throw
    {

        [ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
        public static void OnUpdate(ISystem.Info info, Entity entity, //made with ai assist cause i cant find any example in tflippy code  :( (still works good)
            [Source.Owned] ref Body.Data body,
            [Source.Owned] in Transform.Data transform,
            [Source.Owned] in Control.Data control,
            [Source.Owned] in NPC.Data npc)
        {
            var keyboard = control.keyboard;
            if (keyboard.GetKey(Keyboard.Key.C))
            {
                var ent_item = npc.ent_pickup_target;
                if (!ent_item.IsAlive()) return;

                ref var item_body = ref ent_item.GetComponent<Body.Data>();
                if (item_body.IsNull()) return;

                var dir = (control.mouse.position - transform.position).GetNormalized();

                var force = 250.0f;

                item_body.AddForceWorld(dir * force, item_body.GetPosition());
            }
        }
    }
}