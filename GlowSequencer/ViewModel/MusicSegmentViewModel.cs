using ContinuousLinq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.ViewModel
{
    public class MusicSegmentViewModel : Observable
    {

        private SequencerViewModel sequencer;
        private Model.MusicSegment model;

        public string Label { get { return model.Label; } set { sequencer.ActionManager.RecordSetProperty(model, m => m.Label, value); } }
        public float Bpm { get { return model.Bpm; } set { ChangeBpm(value); } }
        public int BeatsPerBar { get { return model.BeatsPerBar; } set { sequencer.ActionManager.RecordSetProperty(model, m => m.BeatsPerBar, value); } }

        public float TimeOriginSeconds { get { return model.TimeOrigin; } set { ChangeTimeOrigin(value); } }
        public TimeSpan TimeOrigin { get { return TimeSpan.FromSeconds(model.TimeOrigin); } set { TimeOriginSeconds = (float)value.TotalSeconds; } }

        public bool IsReadOnly { get { return model.IsReadOnly; } }
        public bool IsDefault { get { return model.IsDefault; } }

        public ReadOnlyContinuousCollection<object> ReferringBlocksDummies { get; private set; }


        public MusicSegmentViewModel(SequencerViewModel sequencer, Model.MusicSegment model)
        {
            this.sequencer = sequencer;
            this.model = model;

            ReferringBlocksDummies = sequencer.GetModel().Blocks.Where(b => b.SegmentContext == model).Select(b => (object)null);

            ForwardPropertyEvents("Label", model, "Label");
            ForwardPropertyEvents("Bpm", model, "Bpm");
            ForwardPropertyEvents("BeatsPerBar", model, "BeatsPerBar");
            ForwardPropertyEvents("TimeOrigin", model, "TimeOrigin", "TimeOriginSeconds");
            ForwardPropertyEvents("IsReadOnly", model, "IsReadOnly");
            ForwardPropertyEvents("IsDefault", model, "IsDefault");

            ForwardPropertyEvents("Bpm", this, () => sequencer.NotifyGridInterval());
        }

        private void ChangeBpm(float value)
        {
            using (var t = sequencer.ActionManager.CreateTransaction(false))
            {
                float oldValue = model.Bpm;
                sequencer.ActionManager.RecordSetProperty(model, m => m.Bpm, value);
                value = model.Bpm; // adjust for potential clamp

                // do not touch blocks if the setting is disabled
                if (!sequencer.AdjustBlocksWithSegmentChanges)
                    return;

                // sanity check
                if (oldValue <= 0 || value <= 0)
                    return;

                float scaleFactor = oldValue / value;
                bool confirmedWarning = false;

                foreach (var b in sequencer.AllBlocks) // caution: continuous collection
                {
                    if (b.SegmentContext.model != model) continue;

                    float newStartTime = (b.StartTime - model.TimeOrigin) * scaleFactor + model.TimeOrigin;

                    if (!confirmedWarning && newStartTime < 0)
                    {
                        // note: breaking MVVM here by directly displaying message box
                        var result = System.Windows.MessageBox.Show("Stretching associated blocks would cut them off at the beginning, causing a loss of information." + Environment.NewLine + Environment.NewLine +
                                                                    "Are you sure you want to continue?",
                                                                    "Warning", System.Windows.MessageBoxButton.YesNo);
                        if (result != System.Windows.MessageBoxResult.Yes)
                        {
                            t.Rollback();
                            return;
                        }
                        System.Windows.MessageBox.Show("Don't forget to correct the location of the affected blocks at the beginning of the sequence.");
                        confirmedWarning = true;
                    }

                    b.StartTime = newStartTime;
                    b.ScaleDuration(scaleFactor);
                }
            }
        }

        private void ChangeTimeOrigin(float value)
        {
            using (var t = sequencer.ActionManager.CreateTransaction(true))
            {
                float oldValue = model.TimeOrigin;
                sequencer.ActionManager.RecordSetProperty(model, m => m.TimeOrigin, value);

                // do not touch blocks if the setting is disabled
                if (!sequencer.AdjustBlocksWithSegmentChanges)
                    return;

                float delta = value - oldValue;
                bool confirmedWarning = false;

                foreach (var b in sequencer.AllBlocks) // caution: continuous collection
                {
                    if (b.SegmentContext.model != model) continue;

                    if (!confirmedWarning && b.StartTime + delta < 0)
                    {
                        // note: breaking MVVM here by directly displaying message box
                        var result = System.Windows.MessageBox.Show("Moving associated blocks by " + delta.ToString("0.##") + "s would cut them off at the beginning, causing a loss of information." + Environment.NewLine + Environment.NewLine +
                                                                    "Are you sure you want to continue?",
                                                                    "Warning", System.Windows.MessageBoxButton.YesNo);
                        if(result != System.Windows.MessageBoxResult.Yes)
                        {
                            t.Rollback();
                            return;
                        }
                        System.Windows.MessageBox.Show("Don't forget to correct the location of the affected blocks at the beginning of the sequence.");
                        confirmedWarning = true;
                    }

                    b.StartTime += delta;
                }
            }
        }


        public float GetBeatsPerSecond()
        {
            return model.GetBeatsPerSecond();
        }

        public int GetIndex()
        {
            return model.GetIndex();
        }

        public Model.MusicSegment GetModel()
        {
            return model;
        }
    }
}
