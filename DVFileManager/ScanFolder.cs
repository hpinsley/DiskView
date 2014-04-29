using System;
using System.Diagnostics;
using System.IO;
using KayeScholer.DiskView.DVFileManager;

namespace DVFileManager {
    public class ScanFolder : IComparable {
        private DirectoryInfo	_dirInfo = null;
        private	BucketManager	_mgr = new BucketManager();
        private FolderManager	_subfolderMgr = new FolderManager();
        private Bucket			_totalsBucket = null;

        public string FolderName { get { return this._dirInfo.Name; }}
        public string FolderPath { get { return this._dirInfo.FullName; }}
        public string DisplayName {
            get {
                if (Traverser.GlobalCompareType == CompareType.BySize)			
                    return string.Format("{0} {1:#,##0}", FolderName, TotalsBucket.FileSize);
                else if (Traverser.GlobalCompareType == CompareType.ByCount)
                    return string.Format("{0} {1:#,##0}", FolderName, TotalsBucket.FileCount);
                else
                    throw new ApplicationException("Unsupported comparison type in ScanFolder.DisplayName");
            }
        }
        public Bucket TotalsBucket {
            get {
                if (this._totalsBucket == null)
                    this._totalsBucket = this._mgr.ComputeTotalsBucket();

                return this._totalsBucket;
            }
        }

        //Node visitor
        public void Visit(FolderVisitor Visitor) {
            Traverser.Message(this.FolderPath);
            Visitor(this);
            foreach (ScanFolder f in this._subfolderMgr)
                f.Visit(Visitor);
        }
        //Node visitor (2)
        public void Visit2(FolderVisitor2 Visitor, object o) {
            Traverser.Message(this.FolderPath);
            Visitor(this, o);
            foreach (ScanFolder f in this._subfolderMgr)
                f.Visit2(Visitor, o);
        }

        /// <summary>
        /// This method will clear our totals bucket so that it can be recomputed
        /// (and recreated) on demand the next time.
        /// </summary>
        public void ClearTotals() {
            this._totalsBucket = null;
        }

        public BucketManager	BucketMgr { get { return this._mgr; }}
        public FolderManager	SubfolderMgr { get { return this._subfolderMgr; }}

        public ScanFolder(DirectoryInfo DirInfo) {
            this._dirInfo = DirInfo;
        }

        public void Scan() {
            Traverser.Message(this.FolderPath);

            try {
                foreach (FileInfo f in this._dirInfo.GetFiles()) {
                    try {
                        this._mgr.AddFile(f.Extension, f.Length);
                    }
                    catch (System.IO.IOException E) {
                        Trace.WriteLine(string.Format("Ignoring error {0} in folder {1}",
                                                      E.Message, this._dirInfo.FullName));
                    }
                }
                foreach (DirectoryInfo d in this._dirInfo.GetDirectories()) {
                    try {
                        ScanFolder	NewFolder = new ScanFolder(d);
                        this._subfolderMgr.AddFolder(NewFolder);
                        NewFolder.Scan();
                        this._mgr.AddBuckets(NewFolder._mgr);
                    }
                    catch (System.IO.IOException E) {
                        Trace.WriteLine(string.Format("Ignoring error {0} in folder {1}",
                                                      E.Message, this._dirInfo.FullName));
                    }
                }
                //At this point the scan folder has two collections of things that can
                //and should be sorted; subfolders and buckets (containing file types).

                this._mgr.Sort();
                this._subfolderMgr.Sort();
            }
            catch (System.Exception E) {
                Trace.WriteLine(string.Format("Ignoring error {0} in folder {1}",
                                              E.ToString(), this._dirInfo.FullName));
            }
        }


        public void DumpScanFolder() {
            Trace.WriteLine(string.Format("Dump of folder {0}", this._dirInfo.FullName));
            this._mgr.DumpBuckets();
        }
        public void DumpScanFolderRecurse() {
            Trace.WriteLine(string.Format("Dump of folder {0}", this._dirInfo.FullName));
            this._mgr.DumpBuckets();
            this._subfolderMgr.DumpSubfolderList();
        }
        #region IComparable Members

        //Folders compare by comparing their totals buckets.
        public int CompareTo(object obj) {
            if (!(obj is ScanFolder))
                throw new ApplicationException("ScanFolders can only compare to other ScanFolders.");
            ScanFolder Other = (ScanFolder) obj;
            return this.TotalsBucket.CompareTo(Other.TotalsBucket);
        }

        #endregion
    }
}