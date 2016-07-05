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
            ForwardPropertyEvents("FilePath", this, "DocumentName", "DocumentNameDecorated");
            ForwardPropertyEvents("IsDirty", this, "DocumentNameDecorated");
        }

        public void OpenNewDocument()
        {
            FilePath = null;

            Timeline timeline = new Timeline();
            timeline.SetupNew();
            CurrentDocument = new SequencerViewModel(timeline) { CurrentWinWidth = TransferWinWidth() };

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
            CurrentDocument = new SequencerViewModel(timeline) { CurrentWinWidth = TransferWinWidth() };

            IsDirty = false;
            CurrentDocument.ActionManager.CollectionChanged += (_, __) => IsDirty = true;

            return true;
        }

        public bool SaveDocument()
        {
            if (FilePath == null)
                throw new InvalidOperationException("quicksave not available");

            return SaveDocumentAs(FilePath, (FilePath.EndsWith(".xml", StringComparison.InvariantCultureIgnoreCase)));
        }

        public bool SaveDocumentAs(string file, bool compressed)
        {
            if (!FileSerializer.SaveToFile(CurrentDocument.GetModel(), file, compressed))
                return false;

            FilePath = file;
            IsDirty = false;
            return true;
        }


        public bool ExportProgram(string filename)
        {
            if (filename.EndsWith(".glo", StringComparison.InvariantCultureIgnoreCase))
                filename = filename.Substring(0, filename.Length - 4);

            return FileSerializer.ExportGloFiles(CurrentDocument.GetModel(), filename + "_", ".glo");
        }


        private double TransferWinWidth()
        {
            return CurrentDocument != null ? CurrentDocument.CurrentWinWidth : 1000;
        }
    }
}
