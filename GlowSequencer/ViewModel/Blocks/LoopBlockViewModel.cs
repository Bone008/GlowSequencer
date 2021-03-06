﻿using ContinuousLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace GlowSequencer.ViewModel
{
    public class LoopBlockViewModel : GroupBlockViewModel
    {
        protected new Model.LoopBlock model;

        public int Repetitions
        {
            get { return model.Repetitions; }
            set { sequencer.ActionManager.RecordSetProperty(model, m => m.Repetitions, value); }
        }

        public IEnumerable<ReadOnlyContinuousCollection<BlockViewModel>> ChildrenRepetitions { get { return Enumerable.Repeat(Children, Repetitions); } }

        public LoopBlockViewModel(SequencerViewModel sequencer, Model.LoopBlock model)
            : base(sequencer, model, "Loop")
        {
            this.model = model;
            ForwardPropertyEvents(nameof(model.Repetitions), model,
                nameof(Repetitions), nameof(ChildrenRepetitions), nameof(EndTimeOccupied));
        }

    }
}
