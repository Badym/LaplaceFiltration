using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LaplaceFilter
{
    internal static class Program
    {

        
        /// <summary>
        /// Główny punkt wejścia dla aplikacji.
        /// </summary>
        [STAThread]
         
        static void Main()
        {
            // Ustawienia dla aplikacji okienkowej
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            //Uruchomienie okna głównego(Form1)
            Application.Run(new Form1());
        }
    }
}
