using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows.Input;

namespace LumiSnap.VM
{
    public class LumiSnapSettingsVM : INPC
    {
        #region Private
        private string collisionViewName;
        private double distanceRev;
        private double distanceFwd;
        private int selectedIndex;
        private void btnCancel(object obj)
        {
            RevitTransactionResult = Result.Cancelled;
            OnRequestClose(this, new EventArgs());
        }
        private void btnOK(object obj)
        {
            RevitTransactionResult = Result.Succeeded;
            OnRequestClose(this, new EventArgs());
        }

        #endregion

        #region Binding points
        public ObservableCollection<string> CollisionLinkNames
        {
            get
            {
                ObservableCollection<string> R = new ObservableCollection<string>();
                foreach (var item in CollisionLinks)
                {
                    R.Add(item.Name);
                }
                return R;
            }
        }
        public ICommand btn_CANCEL { get; set; }
        public ICommand btn_OK { get; set; }
        public string CollisionViewName
        {
            get => collisionViewName; set
            {
                if (collisionViewName != value)
                {
                    collisionViewName = value;
                    MyPropertyChanged(nameof(CollisionViewName));
                }
            }
        }
        public double DistanceRev
        {
            get => distanceRev; set
            {
                if (distanceRev != value)
                {
                    distanceRev = value;
                    MyPropertyChanged(nameof(DistanceRev));
                }

            }
        }
        public double DistanceFwd
        {
            get => distanceFwd; set
            {
                if (distanceFwd != value)
                {
                    distanceFwd = value;
                    MyPropertyChanged(nameof(DistanceFwd));
                }

            }
        }
        public int SelectedIndex
        {
            get => selectedIndex; set
            {
                if (selectedIndex != value)
                {
                    selectedIndex = value;
                    MyPropertyChanged(nameof(SelectedIndex));
                }

            }
        }

        #endregion

        public event EventHandler OnRequestClose;
        public ObservableCollection<CollisionCatItem> CollisionCatItems { get; set; }
        public List<RevitLinkType> CollisionLinks;
        public Result RevitTransactionResult { get; set; }
        public RevitLinkType SelectedLink
        {
            get
            {
                if (CollisionLinks.Count == 0)
                {
                    return null;
                }
                return CollisionLinks[SelectedIndex];
            }
        }

        public LumiSnapSettingsVM()
        {
            CollisionCatItems = new ObservableCollection<CollisionCatItem>();
            CollisionLinks = new List<RevitLinkType>();

            btn_OK = new RCommand(btnOK);
            btn_CANCEL = new RCommand(btnCancel);
        }

    }

    public class CollisionCatItem : INPC
    {
        private string name;
        private bool selected;

        public string Name
        {
            get => name; set
            {
                if (name != value)
                {
                    name = value;
                    MyPropertyChanged(nameof(Name));
                }

            }
        }

        public bool Selected
        {
            get => selected; set
            {
                if(selected != value)
                {
                    selected = value;
                    MyPropertyChanged(nameof(Selected));
                }
                
            }
        }

    }
     

}
