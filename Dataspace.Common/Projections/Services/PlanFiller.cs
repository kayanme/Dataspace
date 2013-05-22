using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using Dataspace.Common.Interfaces;
using Dataspace.Common.Projections.Classes;

namespace Dataspace.Common.Projections.Services
{
    [Export]
    [PartCreationPolicy(CreationPolicy.Shared)]
    internal sealed class PlanFiller
    {
#pragma warning disable 0649
        [Import]
        private IGenericPool _cachier;
#pragma warning restore 0649

        public FilledProjectionFrame FillFrame(ProjectionFrame frame)
        {
            return FillFrame(frame.RootNode);
        }

        private FilledProjectionFrame FillFrame(FrameNode frame)
        {
            FilledProjectionFrame filledFrame;
            if (frame.MatchedElement is ResourceProjectionElement)
            {
                var filler = _cachier.GetLater(
                    (frame.MatchedElement as ResourceProjectionElement).ResourceType, frame.Key);
                filledFrame = new FilledProjectionFrame(frame.MatchedElement, frame.Key,filler);
            }
            else
            {
                filledFrame = new FilledProjectionFrame(frame.MatchedElement, frame.Key);
            }
            var childFrames = frame.ChildNodes.Select(FillFrame).ToArray();
            filledFrame.ChildFrames = childFrames;
            return filledFrame;
        }

    }
}
