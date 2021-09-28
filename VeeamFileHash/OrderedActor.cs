using System;
using System.Collections.Generic;

namespace VeeamFileHash
{
    public class OrderedActor
    {
        private Dictionary<int, Action> Actions { get; } = new Dictionary<int, Action>();
        private int currentAct = 0;
        private static readonly object lockObj = new object();

        public void DoAct(int actNum, Action act)
        {
            lock (lockObj)
            {
                if (currentAct == actNum)
                {
                    act.Invoke();
                    while (Actions.TryGetValue(++currentAct, out act))
                    {
                        Actions.Remove(currentAct);
                        act.Invoke();
                    }
                }
                else
                    Actions[actNum] = act;
            }
        }
    }
}