﻿using MinimalEmailClient.Events;
using MinimalEmailClient.Models;
using MinimalEmailClient.ViewModels;
using Prism.Events;
using System.Collections.Generic;
using System.Windows.Controls;

namespace MinimalEmailClient.Views
{
    /// <summary>
    /// Interaction logic for MessageListView.xaml
    /// </summary>
    public partial class MessageListView : UserControl
    {
        private MessageListViewModel viewModel;
        private IEventAggregator eventAggregator;

        public MessageListView()
        {
            InitializeComponent();

            viewModel = (MessageListViewModel)this.DataContext;
            this.eventAggregator = GlobalEventAggregator.Instance().EventAggregator;
            this.eventAggregator.GetEvent<DeleteMessagesEvent>().Subscribe(DeleteMessages);
        }

        private void DeleteMessages(string ignoredEventPayload)
        {
            List<Message> selectedMessages = new List<Message>();

            foreach (Message message in messageListView.SelectedItems)
            {
                selectedMessages.Add(message);
            }

            viewModel.DeleteMessages(selectedMessages);
        }
    }
}
