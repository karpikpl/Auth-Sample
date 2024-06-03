namespace desktopClient;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Button loginButton;

    /// <summary>
    ///  Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 450);
        this.Text = "Form1";

        // Create a new Button control
        this.loginButton = new System.Windows.Forms.Button();
        this.loginButton.Location = new System.Drawing.Point(350, 200); // Set the location of the button
        this.loginButton.Size = new System.Drawing.Size(100, 50); // Set the size of the button
        this.loginButton.Text = "Login and Get Data"; // Set the text of the button

        // Add a Click event handler for the button
        this.loginButton.Click += new System.EventHandler(this.LoginButton_Click);

        // Add the button to the form
        this.Controls.Add(this.loginButton);
    }

    #endregion
}
