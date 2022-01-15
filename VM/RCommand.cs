using System;
using System.Windows.Input;

namespace LumiSnap.VM
{
    public class RCommand : ICommand
    {
        /// <summary>
        /// Šajā klasē definēts universāls ICommand implementēts objekts
        /// Šo izmanto, lai iekš ViewModel izveidotu jaunas instances ptiekš ICommand Property.
        /// Papildus norāda izsaucamās komandas funkcijas un izsaukšanas atļaujas pārbaudes vērtību.
        /// </summary>
        public event EventHandler CanExecuteChanged
        {
            // Šis paziņo par atļaujas vērības maiņu.
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Iekšējie objekti funkcijas un tās atļaujas uzglābāšanai.
        /// object parametra vērtība tiek padota no View UI elementa (piem Pogas) caur CommandParameter
        /// </summary>
        private readonly Action<object> _actionToExecute;
        private readonly Func<object, bool> _canExecute;

        /// <summary>
        /// Klases konstruktors, kas izveido jaunas instances izpildāmai funkcijai un atļaujas vērtībai
        /// </summary>
        /// <param name="ActionToExecute"></param>
        /// <param name="CanExecute"></param>
        public RCommand(Action<object> ActionToExecute, Func<object,bool> CanExecute)
        {
            _actionToExecute = ActionToExecute;
            _canExecute = CanExecute;
        }

        /// <summary>
        /// Vienkāršots klases konstruktors, kas izveido jaunu instanci izpildāmai funkcijai
        /// </summary>
        /// <param name="ActionToExecute"></param>
        public RCommand(Action<object> ActionToExecute)
        {
            _actionToExecute = ActionToExecute;
        }

        /// <summary>
        /// Tiek veikta parbaudes funkcijas izpilde.
        /// object parametra vērtība tiek padota no View UI elementa (piem Pogas) caur CommandParameter
        /// Šo object vnk padod tālāk uz ViewModel padoto funkciju.
        /// </summary>
        /// <param name="parameter"></param>
        /// <returns></returns>
        public bool CanExecute(object parameter)
        {
            return _canExecute == null ? true : _canExecute(parameter);

            //    Same as this:

            //if (_canExecute == null)
            //{
            //    return true;
            //}
            //else
            //{
            //    return _canExecute(parameter);
            //}
        }

        /// <summary>
        /// Tiek veikta padotās funkcijas izpilde.
        /// object parametra vērtība tiek padota no View UI elementa (piem Pogas) caur CommandParameter.
        /// Šo object vnk padod tālāk uz ViewModel padoto funkciju.
        /// </summary>
        /// <param name="parameter"></param>
        public void Execute(object parameter)
        {
            _actionToExecute(parameter);
        }
    }
}
