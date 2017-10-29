﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Msagl.Core.Geometry.Curves;
using Microsoft.Msagl.Core.Layout;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.GraphViewerGdi;
using Edge = Microsoft.Msagl.Drawing.Edge;
using Node = Microsoft.Msagl.Drawing.Node;
using Point = Microsoft.Msagl.Core.Geometry.Point;
using Rectangle = System.Drawing.Rectangle;

namespace MsAglWinFormsEditor
{
    /// <summary>
    /// Main project form. Realisation for all user events
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly GViewer viewer = new GViewer();
        private readonly MsAglGraphRepresentation graph = new MsAglGraphRepresentation();
        private readonly Hashtable imagesHashtable = new Hashtable();
        private Node selectedNode;
        private const int fontSize = 11;

        /// <summary>
        /// Create form with given graph
        /// </summary>
        public MainForm()
        {
            viewer.MouseClick += ViewerMouseClicked;
            InitializeComponent();
            viewer.EdgeAdded += ViewerOnEdgeAdded;

            viewer.Graph = graph.Graph;
            viewer.PanButtonPressed = true;
            viewer.MouseMove += (sender, args) =>
            {
                viewer.Graph.GeometryGraph.UpdateBoundingBox();
                viewer.Invalidate();
            };
            viewer.ToolBarIsVisible = false;
            viewer.MouseDown += ViewerOnMouseDown;
            viewer.MouseWheel += (sender, args) => viewer.ZoomF += args.Delta * SystemInformation.MouseWheelScrollLines / 4000f;
            SuspendLayout();
            viewer.Dock = DockStyle.Fill;
            mainLayout.Controls.Add(viewer, 0, 0);
            ResumeLayout();
            InitPalette();
        }

        private void ViewerOnMouseDown(object sender, MouseEventArgs mouseEventArgs)
           => viewer.PanButtonPressed = viewer.SelectedObject == null;

        private void ViewerOnEdgeAdded(object sender, EventArgs eventArgs)
        {
            var edge = (Edge)sender;
            var form = new Form();
            var tableLayout = new TableLayoutPanel { Dock = DockStyle.Fill };
            form.Controls.Add(tableLayout);

            // TODO: GetEdgeTypes or something
            foreach (var type in graph.GetNodeTypes())
            {
                if (type.InstanceMetatype != Repo.Metatype.Edge || type.IsAbstract)
                {
                    continue;
                }

                var edgeType = type as Repo.IEdge;

                tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
                // TODO: We definitely need names for non-abstract edges. Maybe as attribute?
                var associationButton = new Button { Text = "Link", Dock = DockStyle.Fill };
                associationButton.Click += (o, args) =>
                {
                    graph.CreateNewEdge(edgeType, edge);
                    viewer.Graph.AddPrecalculatedEdge(edge);
                    viewer.SetEdgeLabel(edge, edge.Label);
                    var ep = new EdgeLabelPlacement(viewer.Graph.GeometryGraph);
                    ep.Run();
                    viewer.Invalidate();
                    form.DialogResult = DialogResult.OK;
                };
                associationButton.Font = new Font(associationButton.Font.FontFamily, fontSize);
                tableLayout.Controls.Add(associationButton, 0, tableLayout.RowCount - 1);
                ++tableLayout.RowCount;
            }
            var result = form.ShowDialog();
            if (result == DialogResult.Cancel)
            {
                viewer.Undo();
                viewer.Invalidate();
            }
        }

        private void InitPalette()
        {
            foreach (var type in graph.GetNodeTypes())
            {
                var button = new Button { Text = type.Name, Dock = DockStyle.Bottom };
                button.Click += (sender, args) =>
                {
                    var node = graph.CreateNewNode(type);
                    node.GeometryNode = GeometryGraphCreator.CreateGeometryNode(viewer.Graph, viewer.Graph.GeometryGraph, node, ConnectionToGraph.Disconnected);
                    var viewNode = viewer.CreateIViewerNode(node, viewer.Graph.Nodes.ToList()[0].Pos - new Point(250, 0), null);
                    viewer.AddNode(viewNode, true);
                    viewer.Graph.AddNode(node);
                    viewer.Invalidate();
                };
                button.Font = new Font(button.Font.FontFamily, fontSize);
                button.Size = new Size(button.Width, button.Height + 10);
                paletteGrid.Controls.Add(button, 0, paletteGrid.RowCount - 1);

                ++paletteGrid.RowCount;
            }
        }

        private void ViewerMouseClicked(object sender, MouseEventArgs e)
        {
            selectedNode = viewer.SelectedObject as Node;
            if (selectedNode != null)
            {
                var attributes = selectedNode.UserData as List<Repo.IAttribute>;
                if (attributes != null)
                {
                    attributeTable.Visible = true;
                    loadImageButton.Visible = true;
                    viewer.PanButtonPressed = false;
                    var image = imagesHashtable[selectedNode.Id] as Image;
                    if (image != null)
                    {
                        imageLayoutPanel.Visible = true;
                        widthEditor.Value = image.Width;
                        heightEditor.Value = image.Height;
                    }
                    else
                    {
                        imageLayoutPanel.Visible = false;
                    }
                    attributeTable.Rows.Clear();
                    foreach (var attribute in attributes)
                    {
                        object[] row = { attribute.Name, attribute.Kind.ToString(), attribute.StringValue };
                        attributeTable.Rows.Add(row);
                    }
                }
            }
            else
            {
                attributeTable.Visible = false;
                loadImageButton.Visible = false;
                imageLayoutPanel.Visible = false;
                viewer.PanButtonPressed = viewer.SelectedObject == null;
            }
        }

        private void LoadImageButtonClick(object sender, EventArgs e)
        {
            var openImageDialog = new OpenFileDialog { Filter = @"Image files *.png | *.png" };
            if (openImageDialog.ShowDialog() == DialogResult.OK)
            {
                var newImage = new Bitmap(openImageDialog.FileName);
                imagesHashtable[selectedNode.Id] = newImage;
                widthEditor.Value = newImage.Width;
                heightEditor.Value = newImage.Height;
                ImageSizeChanged(null, null);
                imageLayoutPanel.Visible = true;
            }
        }

        private ICurve NodeBoundaryDelegate(Node node)
        {
            var image = (Image)imagesHashtable[node.Id];
            double width = image.Width;
            double height = image.Height;

            return CurveFactory.CreateRectangle(width, height, new Point());
        }

        private GraphicsPath FillTheGraphicsPath(ICurve iCurve)
        {
            var curve = iCurve as Curve;
            var path = new GraphicsPath();
            if (curve != null)
            {
                foreach (var seg in curve.Segments)
                    AddSegmentToPath(seg, ref path);
            }
            return path;
        }

        private void AddSegmentToPath(ICurve seg, ref GraphicsPath p)
        {
            var line = seg as LineSegment;
            if (line != null)
            {
                p.AddLine(PointF(line.Start), PointF(line.End));
            }
        }

        private readonly Font drawFont = new Font("Arial", 10);
        private readonly SolidBrush drawBrush = new SolidBrush(System.Drawing.Color.Black);

        private readonly StringFormat format = new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        private PointF PointF(Point p)
            => new PointF((float)p.X, (float)p.Y);

        private bool DrawNode(Node node, object graphics)
        {
            var graphic = (Graphics)graphics;
            var image = (Image)imagesHashtable[node.Id];
            using (Matrix matrix = graphic.Transform)
            {
                using (Matrix matrixClone = matrix.Clone())
                {
                    graphic.SetClip(FillTheGraphicsPath(node.GeometryNode.BoundaryCurve));
                    using (var matrixMult = new Matrix(1, 0, 0, -1, 0, 2 * (float)node.GeometryNode.Center.Y))
                        matrix.Multiply(matrixMult);

                    graphic.Transform = matrix;
                    graphic.DrawImage(image, new PointF((float)(node.GeometryNode.Center.X - node.GeometryNode.Width / 2),
                        (float)(node.GeometryNode.Center.Y - node.GeometryNode.Height / 2)));
                    var centerX = (float)node.GeometryNode.Center.X;
                    var centerY = (float)node.GeometryNode.Center.Y;
                    graphic.DrawString(node.LabelText, drawFont, drawBrush, new PointF(centerX, centerY), format);
                    graphic.Transform = matrixClone;
                    graphic.ResetClip();
                }
            }
            return true; // Returning false would enable the default rendering
        }

        private void RefreshButtonClick(object sender, EventArgs e)
        {
            viewer.Graph = graph.Graph;
            viewer.Invalidate();
        }

        private void ImageSizeChanged(object sender, EventArgs e)
        {
            var center = selectedNode.GeometryNode.Center;
            var image = imagesHashtable[selectedNode.Id] as Image;
            imagesHashtable[selectedNode.Id] = ResizeImage(image, Convert.ToInt32(widthEditor.Value), Convert.ToInt32(heightEditor.Value));

            selectedNode.Attr.Shape = Shape.DrawFromGeometry;
            selectedNode.DrawNodeDelegate = DrawNode;
            selectedNode.NodeBoundaryDelegate = NodeBoundaryDelegate;
            selectedNode.Edges.ToList()[0].GeometryObject.RaiseLayoutChangeEvent(0);
            viewer.CreateIViewerNode(selectedNode).Node.GeometryNode.Center = center;
            viewer.Graph.GeometryGraph.UpdateBoundingBox();

            viewer.Invalidate();
        }

        private Image ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private void InsertingEdgeCheckedChanged(object sender, EventArgs e)
            => viewer.InsertingEdge = !viewer.InsertingEdge;
    }
}
