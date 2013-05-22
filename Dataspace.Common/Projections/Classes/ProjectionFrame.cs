using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dataspace.Common.Projections.Classes
{
    internal sealed class ProjectionFrame
    {
        public readonly FrameNode RootNode;

        public ProjectionFrame(FrameNode rootNode)
        {
            RootNode = rootNode;
        }
    }
}
