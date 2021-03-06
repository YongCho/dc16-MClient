﻿using MinimalEmailClient.Events;
using MinimalEmailClient.Models;
using MinimalEmailClient.Services;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MinimalEmailClient.ViewModels
{
    public class MailboxTreeViewModel : BindableBase
    {
        public ObservableCollection<AccountViewModel> AccountViewModels { get; set; }
        public ICommand DeleteAccountCommand { get; set; }
        private Mailbox currentMailbox;
        public Mailbox CurrentMailbox
        {
            get { return this.currentMailbox; }
            private set
            {
                SetProperty(ref this.currentMailbox, value);
                GlobalEventAggregator.Instance.GetEvent<MailboxSelectionEvent>().Publish(this.currentMailbox);
            }
        }
        private Account currentAccount;
        public Account CurrentAccount
        {
            get { return this.currentAccount; }
            private set
            {
                SetProperty(ref this.currentAccount, value);
                GlobalEventAggregator.Instance.GetEvent<AccountSelectionEvent>().Publish(this.currentAccount);
            }
        }

        // This could be a MailboxViewModel or an AccountViewModel.
        private object selectedTreeViewItem;
        public object SelectedTreeViewItem
        {
            get { return this.selectedTreeViewItem; }
            set
            {
                SetProperty(ref this.selectedTreeViewItem, value);

                if (value is MailboxViewModel)
                {
                    MailboxViewModel selectedMailboxViewModel = value as MailboxViewModel;
                    if (selectedMailboxViewModel.Mailbox != CurrentMailbox)
                    {
                        CurrentMailbox = selectedMailboxViewModel.Mailbox;
                    }

                    Account selectedMailboxAccount = AccountManager.Instance.GetAccountByName(selectedMailboxViewModel.Mailbox.AccountName);
                    if (selectedMailboxAccount != CurrentAccount)
                    {
                        CurrentAccount = selectedMailboxAccount;
                    }
                }
                else if (value is AccountViewModel)
                {
                    AccountViewModel selectedAccountViewModel = value as AccountViewModel;
                    if (selectedAccountViewModel.Account != CurrentAccount)
                    {
                        CurrentAccount = selectedAccountViewModel.Account;
                        CurrentMailbox = null;
                    }
                }
                else if (value == null)
                {
                    CurrentAccount = null;
                    CurrentMailbox = null;
                }
            }
        }

        public MailboxTreeViewModel()
        {
            GlobalEventAggregator.Instance.GetEvent<NewAccountAddedEvent>().Subscribe(HandleNewAccountAddedEvent, ThreadOption.UIThread);
            GlobalEventAggregator.Instance.GetEvent<AccountDeletedEvent>().Subscribe(HandleAccountDeletedEvent, ThreadOption.UIThread);
            GlobalEventAggregator.Instance.GetEvent<MailboxListSyncFinishedEvent>().Subscribe(HandleMailboxListSyncFinished, ThreadOption.UIThread);
            DeleteAccountCommand = new DelegateCommand<AccountViewModel>(BeginDeleteAccount);

            LoadAccounts();
        }

        private void BeginDeleteAccount(AccountViewModel accountVm)
        {
            if (accountVm != null)
            {
                if (CurrentMailbox.AccountName == accountVm.Account.AccountName)
                {
                    SelectedTreeViewItem = null;
                }
                Task.Run(() => { AccountManager.Instance.DeleteAccount(accountVm.Account); });
            }
        }

        private void HandleNewAccountAddedEvent(Account newAccount)
        {
            if (newAccount == null)
            {
                throw new ArgumentNullException("newAccount");
            }

            AccountViewModel newAccountViewModel = new AccountViewModel(newAccount);
            Application.Current.Dispatcher.Invoke(() =>
            {
                AccountViewModels.Add(newAccountViewModel);
            });
        }

        private void HandleAccountDeletedEvent(string accountName)
        {
            foreach (AccountViewModel accountVm in AccountViewModels)
            {
                if (accountVm.Account.AccountName == accountName)
                {
                    Application.Current.Dispatcher.Invoke(() => { AccountViewModels.Remove(accountVm); });
                    break;
                }
            }
        }

        private void HandleMailboxListSyncFinished(Account account)
        {
            // Don't change selection if an item is already selected.
            if (SelectedTreeViewItem != null)
            {
                return;
            }

            // Select the first mailbox.
            foreach (AccountViewModel accountVm in AccountViewModels)
            {
                if (accountVm.Account.AccountName == account.AccountName)
                {
                    Application.Current.Dispatcher.Invoke(() => {
                        if (accountVm.MailboxViewModelTree.Count > 0)
                        {
                            accountVm.MailboxViewModelTree[0].IsSelected = true;
                        }
                    });
                    break;
                }
            }
        }

        public void LoadAccounts()
        {
            if (AccountViewModels == null)
            {
                AccountViewModels = new ObservableCollection<AccountViewModel>();
            }
            else
            {
                AccountViewModels.Clear();
            }

            List<Account> accounts = AccountManager.Instance.Accounts;
            foreach (Account acc in accounts)
            {
                AccountViewModels.Add(new AccountViewModel(acc));
            }

            // Select the first mailbox of the first account by default.
            if (AccountViewModels.Count > 0 && AccountViewModels[0].MailboxViewModelTree.Count > 0)
            {
                AccountViewModels[0].MailboxViewModelTree[0].IsSelected = true;
            }
        }
    }
}
