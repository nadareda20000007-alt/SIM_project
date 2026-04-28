package bank;
import java.util.ArrayList;
import java.util.Scanner;

public class Bank {

        public static void main(String[] args) {
        Bank myBank = new Bank("BlackBox Bank");
        myBank.run();
    }
        
    private String bankName;
    private ArrayList<Account> accounts;
    private TransactionFeeCalculator feeCalculator;

    public Bank(String bankName) {
        this.bankName = bankName;
        this.accounts = new ArrayList<>();
        this.feeCalculator = new StandardFeeCalculator(5.0, 0.5);
    }

    public void addAccount(Account account) {
        accounts.add(account);
        System.out.println("Account added successfully!");
    }

    public Account findAccount(String accountNumber) {
        for (Account acc : accounts) {
            if (acc.getAccountNumber().equals(accountNumber)) {
                return acc;
            }
        }
        return null;
    }

    public void displayAllAccounts() {
        System.out.println("\n=== All Accounts ===");
        for (Account acc : accounts) {
            acc.displayAccountInfo();
            System.out.println(acc.toString());
            System.out.println("-------------------");
        }
    }

    public void processTransaction() {
        Scanner sc = new Scanner(System.in);
        System.out.print("Enter account number: ");
        String accNum = sc.nextLine();
        
        Account account = findAccount(accNum);
        if (account == null) {
            System.out.println("Account not found!");
            return;
        }

        System.out.print("Enter amount: ");
        double amount = sc.nextDouble();
        
        // Check transaction fee
        if (feeCalculator.isFeeApplicable(amount)) {
            double fee = feeCalculator.calculateFee(amount);
            System.out.println("Transaction fee: $" + fee);
            amount += fee;
        }

        System.out.println("1. Deposit  2. Withdraw");
        int choice = sc.nextInt();
        
        if (choice == 1) {
            account.deposit(amount);
        } else if (choice == 2) {
            account.withdraw(amount);
        }
        
        account.displayTransactionHistory();
    }

    public void run() {
        Scanner sc = new Scanner(System.in);
        while (true) {
            System.out.println("\n=== " + bankName + " Banking System ===");
            System.out.println("1. Add New Account");
            System.out.println("2. Display All Accounts");
            System.out.println("3. Process Transaction");
            System.out.println("4. Apply Interest (Savings)");
            System.out.println("5. Exit");
            
            System.out.print("Choose option: ");
            int choice = sc.nextInt();
            sc.nextLine(); // consume newline

            switch (choice) {
                case 1:
                    createNewAccount(sc);
                    break;
                case 2:
                    displayAllAccounts();
                    break;
                case 3:
                    processTransaction();
                    break;
                case 4:
                    applyInterestToAllSavings();
                    break;
                case 5:
                    System.out.println("Thank you for banking with " + bankName + "!");
                    return;
                default:
                    System.out.println("Invalid option!");
            }
        }
    }

    private void createNewAccount(Scanner sc) {
        System.out.print("Account Number: ");
        String accNum = sc.nextLine();
        System.out.print("Account Holder Name: ");
        String name = sc.nextLine();
        System.out.print("Initial Balance: ");
        double balance = sc.nextDouble();
        sc.nextLine();

        System.out.println("Account Type: 1.Savings 2.Current");
        int type = sc.nextInt();
        sc.nextLine();

        if (type == 1) {
            System.out.print("Interest Rate (%): ");
            double rate = sc.nextDouble();
            addAccount(new SavingsAccount(accNum, name, balance, rate));
        } else {
            System.out.print("Overdraft Limit: ");
            double limit = sc.nextDouble();
            addAccount(new CurrentAccount(accNum, name, balance, limit));
        }
    }

    private void applyInterestToAllSavings() {
        for (Account acc : accounts) {
            if (acc instanceof SavingsAccount) {
                SavingsAccount savings = (SavingsAccount) acc;
                savings.applyMonthlyInterest();
                System.out.println("Interest applied to " + savings.getAccountHolderName());
            }
        }
    }
}
    
    
    

