﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Configuration.DkimSigner.Exchange;

namespace Configuration.DkimSigner
{
	public partial class ExchangeTransportServiceWindow : Form
	{
		// ##########################################################
		// ##################### Variables ##########################
		// ##########################################################

		private List<TransportServiceAgent> installedAgentsList;
		private int currentAgentPriority;

		// ##########################################################
		// ##################### Construtor #########################
		// ##########################################################

		public ExchangeTransportServiceWindow()
		{
			InitializeComponent();
		}

		// ##########################################################
		// ####################### Events ###########################
		// ##########################################################

		private void ExchangeTransportServiceWindows_Load(object sender, EventArgs e)
		{
			RefreshTransportServiceAgents();
		}

		private void dgvTransportServiceAgents_SelectionChanged(object sender, EventArgs e)
		{
			bool dkimAgentSelected = dgvTransportServiceAgents.SelectedRows.Count == 1 && dgvTransportServiceAgents.SelectedRows[0].Cells["dgvcName"].Value.ToString().Equals(Constants.DkimSignerAgentName);
			btUninstall.Enabled = dkimAgentSelected;
			btDisable.Enabled = dkimAgentSelected;
			RefreshMoveButtons(dkimAgentSelected);
		}

		// ##########################################################
		// ################# Internal functions #####################
		// ##########################################################

		private void RefreshTransportServiceAgents()
		{
			installedAgentsList = null;

			try
			{
				installedAgentsList = ExchangeServer.GetTransportServiceAgents();
			}
			catch (Exception ex)
			{
				MessageBox.Show(this, "Error reading transport agents. " + ex.Message, "Error", MessageBoxButtons.OK,
					MessageBoxIcon.Error);
				return;
			}

			dgvTransportServiceAgents.Rows.Clear();

			if (installedAgentsList != null)
			{
				foreach (TransportServiceAgent oAgent in installedAgentsList)
				{
					dgvTransportServiceAgents.Rows.Add(oAgent.Priority, oAgent.Name, oAgent.Enabled);
					if (oAgent.Name == Constants.DkimSignerAgentName)
						currentAgentPriority = oAgent.Priority;
				}
			}
			foreach (DataGridViewRow row in dgvTransportServiceAgents.Rows)
			{
				row.Selected = row.Cells["dgvcName"].Value.ToString().Equals(Constants.DkimSignerAgentName);
			}

			bool isDkimAgentTransportInstalled = ExchangeServer.IsDkimAgentTransportInstalled();
			bool isDkimAgentTransportEnabled = isDkimAgentTransportInstalled && ExchangeServer.IsDkimAgentTransportEnabled();
			btDisable.Text = (isDkimAgentTransportEnabled ? "Disable" : "Enable");
			RefreshMoveButtons(true);
		}


		private void RefreshMoveButtons(bool isEnabled)
		{
			if (isEnabled && (dgvTransportServiceAgents.SelectedRows.Count >= 1))
			{
				btMoveUp.Enabled = dgvTransportServiceAgents.RowCount > 0 && dgvTransportServiceAgents.SelectedRows[0].Cells["dgvcName"].RowIndex > 0;
				btMoveDown.Enabled = dgvTransportServiceAgents.RowCount > 0 && dgvTransportServiceAgents.SelectedRows[0].Cells["dgvcName"].RowIndex < (dgvTransportServiceAgents.RowCount - 1);
			}
			else
			{
				btMoveUp.Enabled = false;
				btMoveDown.Enabled = false;
			}
		}

		// ###########################################################
		// ###################### Button click #######################
		// ###########################################################

		private void btRefresh_Click(object sender, EventArgs e)
		{
			RefreshTransportServiceAgents();
		}

		private void btUninstall_Click(object sender, EventArgs e)
		{
			if (MessageBox.Show(this, "Do you really want to UNINSTALL the DKIM Exchange Agent?\n", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
			{
				try
				{
					ExchangeServer.UninstallDkimTransportAgent();
					RefreshTransportServiceAgents();
					TransportService ts = new TransportService();
					try
					{
						ts.Do(TransportServiceAction.Restart, delegate (string msg)
						{
							MessageBox.Show(msg, "Service error", MessageBoxButtons.OK, MessageBoxIcon.Error);
						});
					}
					catch (Exception ex)
					{
						MessageBox.Show("Couldn't restart MSExchangeTransport Service. Please restart it manually. \n" + ex.Message, "Error restarting Service", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
					finally
					{
						ts.Dispose();

					}
					MessageBox.Show(this, "Transport Agent unregistered from Exchange. Please remove the folder manually: '" + Constants.DkimSignerPath + "'\nWARNING: If you remove the folder, keep a backup of your settings and keys!", "Uninstalled", MessageBoxButtons.OK, MessageBoxIcon.Information);

				}
				catch (ExchangeServerException ex)
				{
					MessageBox.Show(this, ex.Message, "Uninstall error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
			}
		}

		private void btDisable_Click(object sender, EventArgs e)
		{
			try
			{
				if (btDisable.Text == "Disable")
				{
					ExchangeServer.DisableDkimTransportAgent();
				}
				else
				{
					ExchangeServer.EnableDkimTransportAgent();
				}

				RefreshTransportServiceAgents();
				RefreshMoveButtons(true);

				TransportService ts = new TransportService();
				try
				{
					ts.Do(TransportServiceAction.Restart, delegate (string msg)
					{
						MessageBox.Show(msg, "Service error", MessageBoxButtons.OK, MessageBoxIcon.Error);
					});
				}
				catch (Exception ex)
				{
					MessageBox.Show("Couldn't restart MSExchangeTransport Service. Please restart it manually. \n" + ex.Message, "Error restarting Service", MessageBoxButtons.OK, MessageBoxIcon.Error);
				}
				finally
				{
					ts.Dispose();
				}
			}
			catch (ExchangeServerException ex)
			{
				MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void btMoveUp_Click(object sender, EventArgs e)
		{
			try
			{
				ExchangeServer.SetPriorityDkimTransportAgent(currentAgentPriority - 1);
				RefreshTransportServiceAgents();
			}
			catch (ExchangeServerException ex)
			{
				MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void btMoveDown_Click(object sender, EventArgs e)
		{
			try
			{
				ExchangeServer.SetPriorityDkimTransportAgent(currentAgentPriority + 1);
				RefreshTransportServiceAgents();
			}
			catch (ExchangeServerException ex)
			{
				MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void btClose_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}