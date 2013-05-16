using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace TempView
{
    public enum TimeFormat
    {
        Seconds,
        Minutes,
        Hours
    }

    //=====================================================================================
    /// <summary>
    /// Class to draw a basic graph inside a PictureBox control
    /// </summary>
    //=====================================================================================
    public class Graph
    {
        protected Graphics m_surface;
        protected Image m_image;
        protected PictureBox m_targetImage;
        protected Rectangle m_clientRectangle;
        protected Rectangle m_gridRectangle;

        private Brush m_graphBackGroundBrush = new SolidBrush(Color.White);
        private Pen m_gridPen = new Pen(Color.Black);
        private Pen m_plotPen = new Pen(Color.Red);
        private Brush m_tickLabelBrush = new SolidBrush(Color.Black);

        private Font m_tickLabelFont = new Font("Arial", 8, FontStyle.Regular);
        private int m_xLabelVAdjust;
        private int m_xLabelHAdjust;
        private int m_yLabelVAdjust;
        private int m_yLabelHAdjust;
        private int m_yLabelHeight;
        private int m_yLabelWidth;
        private int m_xMax;
        private double m_yMin;
        private double m_yMax;
        private bool m_yScaleSet;
        private bool m_yScaleChanged;
        private double m_minYValue;
        private double m_maxYValue;
        private List<double> m_tempValues = new List<double>();
        private List<Point> m_points = new List<Point>();
        private int m_sampleCount;
        private int m_samplePeriod;
        private TimeFormat m_timeFormat = TimeFormat.Minutes;
        private string m_appName;

        //==================================================================================
        /// <summary>
        /// ctor - creates a graphics object to use for drawing
        /// </summary>
        /// <param name="targetImage">The picture box to draw in</param>
        //==================================================================================
        public Graph(PictureBox targetImage, string appName)
        {
            m_clientRectangle = targetImage.ClientRectangle;
            m_image = new Bitmap(m_clientRectangle.Width, m_clientRectangle.Height);
            m_surface = Graphics.FromImage(m_image);

            m_xLabelVAdjust = (int)(0.012 * m_clientRectangle.Height);
            m_xLabelHAdjust = (int)(0.008 * m_clientRectangle.Width);
            m_yLabelVAdjust = (int)(0.039 * m_clientRectangle.Height);
            m_yLabelHAdjust = (int)(0.07 * m_clientRectangle.Width);
            m_yLabelWidth = (int)(0.07 * m_clientRectangle.Width);
            m_yLabelHeight = (int)(0.058 * m_clientRectangle.Height);

            m_xMax = 600;
            m_yMin = 0.0;
            m_yMax = 100.0;

            if (m_timeFormat == TimeFormat.Hours)
                m_xMax *= 60;

            m_targetImage = targetImage;
            m_appName = appName;
        }

        //===============================================================================================
        /// <summary>
        /// Initializes graph values. This method should be called by the
        /// application before calling the DrawGrid and DrawPlot methods
        /// </summary>
        /// <param name="samplePeriod">The sample period in seconds (used for x axis scaling)</param>
        //===============================================================================================
        public void IntializeGraph(int samplePeriod, TimeFormat timeFormat)
        {
            m_samplePeriod = samplePeriod;
            m_timeFormat = timeFormat;

            m_minYValue = Double.MaxValue;
            m_maxYValue = Double.MinValue;
            m_yMin = 0.0;
            m_yMax = 100.0;
            m_sampleCount = 0;
            m_yScaleSet = false;
            m_points.Clear();
            m_tempValues.Clear();
        }

        //===============================================================================================
        /// <summary>
        /// Draws the graph's grid and tick lables. X and y axes get 10 grid spaces each.
        /// </summary>
        //===============================================================================================
        public void DrawGrid()
        {
            m_surface.FillRectangle(m_graphBackGroundBrush, m_clientRectangle);

            m_gridRectangle = m_clientRectangle;
            m_gridRectangle.Inflate((int)(-0.07 * m_clientRectangle.Width), (int)(-0.08 * m_clientRectangle.Height));
            m_surface.DrawRectangle(m_gridPen, m_gridRectangle);

            int verticalSpacing = m_gridRectangle.Width / 10;
            int horizontalSpacing = m_gridRectangle.Height / 10;
            Point p1 = Point.Empty;
            Point p2 = Point.Empty;

            // draw vertical lines
            for (int i = 1; i < 10; i++)
            {
                p1.X = m_gridRectangle.Left + (i * verticalSpacing);
                p1.Y = m_gridRectangle.Bottom;
                p2.X = p1.X;
                p2.Y = m_gridRectangle.Top;
                m_surface.DrawLine(m_gridPen, p1.X, p1.Y, p2.X, p2.Y);
            }

            // draw horizontal lines
            for (int i = 1; i < 10; i++)
            {
                p1.X = m_gridRectangle.Left;
                p1.Y = m_gridRectangle.Top + (i * horizontalSpacing);
                p2.X = m_gridRectangle.Right;
                p2.Y = m_gridRectangle.Top + (i * horizontalSpacing);
                m_surface.DrawLine(m_gridPen, p1.X, p1.Y, p2.X, p2.Y);
            }

            // draw x axis tick labels
            p1.X = m_gridRectangle.Left - m_xLabelHAdjust;
            p1.Y = m_gridRectangle.Bottom + m_xLabelVAdjust;

            int timeIncrement = m_xMax / 10;
            double tempIncrement = (m_yMax - m_yMin) / 10;
            double xTickValue;

            for (int i = 0; i < 11; i++)
            {
                xTickValue = i * timeIncrement;

                if (m_timeFormat == TimeFormat.Minutes)
                    xTickValue /= 60;
                else if (m_timeFormat == TimeFormat.Hours)
                    xTickValue /= 3600;

                m_surface.DrawString(xTickValue.ToString(), m_tickLabelFont, m_tickLabelBrush, p1.X, p1.Y);

                p1.X += verticalSpacing;
            }

            // draw y axis tick lables
            Rectangle textRC = Rectangle.Empty;
            textRC.X = m_gridRectangle.Left - m_yLabelHAdjust;
            textRC.Y = m_gridRectangle.Bottom - m_yLabelVAdjust;
            textRC.Width = m_yLabelWidth;
            textRC.Height = m_yLabelHeight;

            StringFormat textFormat = new StringFormat(StringFormatFlags.NoClip);// | StringFormatFlags.FitBlackBox);
            textFormat.Alignment = StringAlignment.Far;

            double yTickValue = m_yMin;

            for (int i = 0; i < 11; i++)
            {
                //g.DrawRectangle(m_gridPen, textRC);
                if (yTickValue % 1 == 0)
                    m_surface.DrawString(yTickValue.ToString(), m_tickLabelFont, m_tickLabelBrush, textRC, textFormat);
                yTickValue += tempIncrement;
                textRC.Y -= horizontalSpacing;
            }

            m_targetImage.Image = m_image;
        }

        //===============================================================================================
        /// <summary>
        /// Updates the plot with the current value
        /// </summary>
        /// <param name="currentValue">The current value</param>
        //===============================================================================================
        public void DrawPlot(double currentValue)
        {
            Point newPoint = Point.Empty;

            m_sampleCount++;

            if (m_samplePeriod * m_sampleCount > m_xMax)
            {
                // if the plot reaches the end of the graph re-scale the graph by doubling the x max value

                // rescale the x axis
                m_xMax *= 2;
                DrawGrid();
                Point[] points = m_points.ToArray();
                m_points.Clear();

                foreach (Point p in points)
                {
                    int x = p.X - m_gridRectangle.X;
                    x /= 2;
                    x += m_gridRectangle.X;
                    m_points.Add(new Point(x, p.Y));
                }
            }

            if (currentValue > m_maxYValue)
                m_maxYValue = currentValue;

            if (currentValue < m_minYValue)
                m_minYValue = currentValue;

            CalculateYAxisScale();

            // x value
            int time = (m_sampleCount - 1) * m_samplePeriod;
            newPoint.X = m_gridRectangle.Left + (int)(((double)time / m_xMax) * m_gridRectangle.Width);

            // y value
            double scaleFactor = (m_yMax - m_yMin) / m_gridRectangle.Height; // (deg / pixel)

            if (m_yScaleChanged)
            {
                // re-scale previously plotted points
                int t;
                Point previousPoint;
                m_points.Clear();

                for (int i = 0; i < m_tempValues.Count; i++)
                {
                    previousPoint = Point.Empty;
                    t = i * m_samplePeriod;
                    previousPoint.X = m_gridRectangle.Left + (int)(((double)t / m_xMax) * m_gridRectangle.Width);
                    previousPoint.Y = m_gridRectangle.Bottom - (int)((m_tempValues[i] - m_yMin) / scaleFactor);
                    m_points.Add(previousPoint);
                }
            }

            newPoint.Y = m_gridRectangle.Bottom - (int)((currentValue - m_yMin) / scaleFactor);

            try
            {
                m_points.Add(newPoint);

                if (m_points.Count > 1)
                {
                    m_surface.DrawLines(m_plotPen, m_points.ToArray());
                    m_targetImage.Image = m_image;
                }
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                MessageBox.Show(e.Message, m_appName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
            }

            m_tempValues.Add(currentValue);
        }

        public Image GetImage()
        {
            return m_image;
        }

        //================================================================================
        /// <summary>
        /// Auto scales the Y axis based on the min and max y values
        /// </summary>
        //================================================================================
        protected void CalculateYAxisScale()
        {
            m_yScaleChanged = false;

            if (!m_yScaleSet || m_minYValue < m_yMin || m_maxYValue > m_yMax)
            {
                double upper;
                double lower;

                upper = Math.Ceiling(m_maxYValue);
                while (upper % 5 != 0)
                    upper++;

                lower = Math.Floor(m_minYValue);
                while (lower % 5 != 0)
                    lower--;

                m_yMax = upper;
                m_yMin = lower;

                DrawGrid();

                m_yScaleSet = true;
                m_yScaleChanged = true;
            }
        }
    }
}
