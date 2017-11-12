using GlowSequencer.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlowSequencer.ViewModel
{
    public class MainViewModel : Observable
    {
        
        // full path of the file the sequence was last saved to, null if not saved yet
        private string _currentFilePath = null;

        private bool _dirty = false;
        private SequencerViewModel _currentDocument = null;


        public string FilePath { get { return _currentFilePath; } private set { SetProperty(ref _currentFilePath, value); } }
        public bool IsDirty { get { return _dirty; } set { SetProperty(ref _dirty, value); } }

        public string DocumentName { get { return (FilePath == null ? "Untitled" : System.IO.Path.GetFileName(FilePath)); } }
        public string DocumentNameDecorated { get { return DocumentName + (IsDirty ? "*" : ""); } }


        public SequencerViewModel CurrentDocument { get { return _currentDocument; } private set { SetProperty(ref _currentDocument, value); } }


        public MainViewModel()
        {
            OpenNewDocument();
            ForwardPropertyEvents(nameof(FilePath), this, nameof(DocumentName), nameof(DocumentNameDecorated));
            ForwardPropertyEvents(nameof(IsDirty), this, nameof(DocumentNameDecorated));
        }

        public void OpenNewDocument()
        {
            FilePath = null;

            Timeline timeline = new Timeline();
            timeline.SetupNew();
            CurrentDocument = MakeSequencerViewModel(timeline);

            IsDirty = false;
            CurrentDocument.ActionManager.CollectionChanged += (_, __) => IsDirty = true;

#if DEBUG
            timeline.SetupTestData();
            CurrentDocument.SelectBlock(CurrentDocument.AllBlocks[0], CompositionMode.None);
#endif
        }

        public bool OpenDocument(string file)
        {
            Timeline timeline = FileSerializer.LoadFromFile(file);
            if(timeline == null)
            {
                OpenNewDocument();
                return false;
            }

            FilePath = file;
            CurrentDocument = MakeSequencerViewModel(timeline);

            IsDirty = false;
            CurrentDocument.ActionManager.CollectionChanged += (_, __) => IsDirty = true;

            return true;
        }

        public bool SaveDocument()
        {
            if (FilePath == null)
                throw new InvalidOperationException("quicksave not available");

            return SaveDocumentAs(FilePath, !FilePath.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase));
        }

        public bool SaveDocumentAs(string file, bool compressed)
        {
            if (!FileSerializer.SaveToFile(CurrentDocument.GetModel(), file, compressed))
                return false;

            FilePath = file;
            IsDirty = false;
            return true;
        }


        public bool ExportProgram(string filename, float startTime)
        {
            if (filename.EndsWith(".glo", StringComparison.InvariantCultureIgnoreCase))
                filename = filename.Substring(0, filename.Length - 4);

            return FileSerializer.ExportGloFiles(CurrentDocument.GetModel(), filename + "_", ".glo", startTime);
        }
        

        private SequencerViewModel MakeSequencerViewModel(Timeline timeline)
        {
            // Hack: Extract view state from old view model and inject into new one,
            // so it can work with it without before the view is updated.
            double viewportLeftOffsetPx = CurrentDocument?.GetViewportLeftOffsetPx() ?? 0.0;
            double viewportWidthPx = CurrentDocument?.GetViewportWidth() ?? 1000.0;

            var newDocument = new SequencerViewModel(timeline);
            newDocument.SetViewportState(viewportLeftOffsetPx, viewportWidthPx);
            return newDocument;
        }
    }
}
