using System;
using System.Data.Entity;   
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace Lab5bS2541
{
    /// <summary>
    /// Windows Forms that displays Doctor Who data using Entity Framework and LINQ.
    /// Left side: doctor selector + details & photo.
    /// Right sid: companions list 
    /// </summary>
    public partial class FormDoctorAndCompanions : Form
    {
        private COMP10204W25_LAB5Entities dbContext;
        /// <summary>
        /// Initializes the Doctor/Companion form
        /// </summary>
        public FormDoctorAndCompanions()
        {
            InitializeComponent();
            CreateMenu();
            //readonly field in the form
            txtPlayedBy.ReadOnly = txtYear.ReadOnly = txtSeries.ReadOnly = txtAge.ReadOnly = txtFirstEpisode.ReadOnly = true;
            txtPlayedBy.TabStop = txtYear.TabStop = txtSeries.TabStop = txtAge.TabStop = txtFirstEpisode.TabStop = false;
            // Configure ListBox line in a way to have sapce
            lstCompanions.DrawMode = DrawMode.OwnerDrawFixed;
            // Fixed height:2 text lines + extra gap 
            lstCompanions.ItemHeight = (int)(lstCompanions.Font.GetHeight() * 2) + 14;
            // Draw each item in lsitbox
            lstCompanions.DrawItem += (s, e) =>
            {
                if (e.Index < 0) return;
                using (var b = new SolidBrush(SystemColors.Window))
                    e.Graphics.FillRectangle(b, e.Bounds);
                string text = (string)lstCompanions.Items[e.Index];
                var rect = new Rectangle(e.Bounds.X + 4, e.Bounds.Y + 3,
                                         e.Bounds.Width - 8, e.Bounds.Height - 6);
                TextRenderer.DrawText(e.Graphics, text, lstCompanions.Font, rect,
                                      SystemColors.WindowText, TextFormatFlags.WordBreak);
            };
            picDoctor.SizeMode = PictureBoxSizeMode.Zoom;
            this.Load += FormDoctorAndCompanions_Load;
            this.cmbDoctor.SelectedIndexChanged += cmbDoctor_SelectedIndexChanged;

            cmbDoctor.DropDownStyle = ComboBoxStyle.DropDownList;
            lstCompanions.HorizontalScrollbar = true;
        }
        /// <summary>
        /// Creates the form's main menu 
        /// </summary>
        private void CreateMenu()
        {
            //create menustrip
            var menu = new MenuStrip();
            var file = new ToolStripMenuItem("&File");

            // Exit with Ctrl+Q
            var exitItem = new ToolStripMenuItem("E&xit", image: null, onClick: (s, e) => this.Close())
            {
                ShortcutKeys = Keys.Control | Keys.Q,
                ShowShortcutKeys = true
            };

            file.DropDownItems.Add(exitItem);
            menu.Items.Add(file);

            this.MainMenuStrip = menu;   // mark as the primary menu for the form
            this.Controls.Add(menu);     // display it
        }
        /// <summary>
        /// Form load handler.
        /// Creates the DbContext, fetches doctors, and binds the ComboBox to show numbers 1 to 13.
        /// </summary>
        /// <param name="sender">The control that raised the event</param>
        /// <param name="e">An event arguments for the Click event</param>
        private void FormDoctorAndCompanions_Load(object sender, EventArgs e)
        {
            dbContext = new COMP10204W25_LAB5Entities();

            // get ids 
            var doctorRows = dbContext.DOCTORs
                                      .OrderBy(d => d.DOCTORID)
                                      .Select(d => new { d.DOCTORID, d.ACTOR })
                                      .ToList(); 

            // display just the number 1to13
            cmbDoctor.DisplayMember = "DOCTORID";
            cmbDoctor.ValueMember = "DOCTORID";
            cmbDoctor.DataSource = doctorRows;
            if (cmbDoctor.Items.Count > 0)
            {
                cmbDoctor.SelectedIndex = 0;
                // ensure details populate on first load
                cmbDoctor_SelectedIndexChanged(cmbDoctor, EventArgs.Empty);
            }
        }
        /// <summary>
        /// execute the selected doctor changes
        /// </summary>
        /// <param name="sender">The control that raised the event</param>
        /// <param name="e">An event arguments for the Click event</param>
        private void cmbDoctor_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!(cmbDoctor.SelectedValue is int doctorId)) return;

            // Load the doctor and its episode 
            var doc = dbContext.DOCTORs
                .Include(d => d.EPISODE)
                .FirstOrDefault(d => d.DOCTORID == doctorId);
            if (doc == null) return; 
            // Left-side fields
            txtPlayedBy.Text = doc.ACTOR;
            txtSeries.Text = doc.SERIES.ToString();
            txtAge.Text = doc.AGE.ToString();
            txtYear.Text = doc.EPISODE.SEASONYEAR.ToString();
            txtFirstEpisode.Text = doc.EPISODE.TITLE;
            // Photo 
            picDoctor.Image = null;
            try
            {
                using (var ms = new MemoryStream(doc.PICTURE))
                    picDoctor.Image = Image.FromStream(ms);
            }
            catch (Exception ex){ 
                Console.WriteLine(ex.Message); 
            }
            // Companions 
            var companions = dbContext.COMPANIONs
                .Where(c => c.DOCTORID == doctorId)
                .Include(c => c.EPISODE)
                .OrderBy(c => c.EPISODE.SEASONYEAR) //orderby year
                .Select(c => new
                {
                    c.NAME,
                    c.ACTOR,
                    Title = c.EPISODE.TITLE,
                    Year = c.EPISODE.SEASONYEAR
                })
                .ToList();
            var lines = companions
                .Select(x => $"{x.NAME} ({x.ACTOR})\r\n\"{x.Title}\" ({x.Year})")
                .ToList();

            lstCompanions.DataSource = lines;
            lstCompanions.Focus();

        }
        /// <summary>
        /// Ensures the DbContext is disposed when the form closes.
        /// </summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            dbContext?.Dispose();
            base.OnFormClosed(e);
        }
    }
}
