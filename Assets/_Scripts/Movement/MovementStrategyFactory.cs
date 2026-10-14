using System.Collections.Generic;
using UnityEngine;

namespace Movement
{
    public class MovementStrategyFactory
    {
        private static MovementStrategyFactory instance;
        private Dictionary<ObjectType, IMovementStrategy> strategies;

        public static MovementStrategyFactory Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MovementStrategyFactory();
                }
                return instance;
            }
        }

        private MovementStrategyFactory()
        {
            strategies = new Dictionary<ObjectType, IMovementStrategy>
            {
                { ObjectType.Rollable, new RollingMovementStrategy() },
                { ObjectType.Jumpable, new JumpMovementStrategy() },
            };
        }

        public IMovementStrategy GetStrategy(ObjectType objType)
        {
            if (objType.HasFlag(ObjectType.Rollable))
            {
                return strategies[ObjectType.Rollable];
            }
            if (objType.HasFlag(ObjectType.Jumpable))
            {
                return new JumpMovementStrategy();
            }

            return new SimpleMovementStrategy();
        }


        public void RegisterStrategy(ObjectType objType, IMovementStrategy strategy)
        {
            strategies[objType] = strategy;
        }
    }
}