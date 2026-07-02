using System;
using Statlyn.Core.Abstractions;

namespace Statlyn.UI
{
    public static class BindingPolicy
    {
        public static void AssertBindable(object viewModel)
        {
            if (viewModel is IRawFootballEntity)
            {
                throw new InvalidOperationException("UI binding to raw football entities is forbidden. Bind masked view models only.");
            }
        }
    }
}
