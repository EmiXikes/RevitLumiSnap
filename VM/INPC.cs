using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LumiSnap.VM
{

    /// <summary>
    /// Šī klase ir kopējā klase, kas paziņo par Property izmaiņām.
    /// Šiet tiek implementēts INotifyPropertyChanged, kas paziņo par izmaiņām
    /// </summary>
    public class INPC : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Šo funkciju jāizsauc manuāli un jānorāda Property nosaukums, kurš tiek mainīts.
        /// Tālāk INotifyPropertyChanged par šīm izmaiņām informē Binding linkus, kas atjauno attiecīgo UI.
        /// </summary>
        /// <param name="PropertyName"></param>
        public void MyPropertyChanged(string PropertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }
    }
}
