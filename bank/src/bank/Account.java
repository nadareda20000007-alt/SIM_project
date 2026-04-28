package bank;
import java.util.ArrayList;
 
public abstract class Account {
    protected String accountNumber;
    protected String accountHolderName;
    protected double balance;
    protected ArrayList<Transaction> transactions;      
    

public String getAccountNumber() { 
    return accountNumber; 
}
public String getAccountHolderName() { 
    return accountHolderName; 
}

 public Account(String accountNumber, String accountHolderName, double initialBalance) {
        this.accountNumber = accountNumber;
        this.accountHolderName = accountHolderName;
        this.balance = initialBalance;
        this.transactions = new ArrayList<>();
    }

     // Abstract methods - must be implemented by subclasses
    public abstract void deposit(double amount);
    public abstract boolean withdraw(double amount);
    public abstract double calculateInterest();

    // Common methods
    public void displayAccountInfo() {
        System.out.println("Account Number: " + accountNumber);
        System.out.println("Account Holder: " + accountHolderName);
        System.out.println("Balance: $" + String.format("%.2f", balance));
    }
    
        public void addTransaction(String type, double amount, double newBalance) {
        transactions.add(new Transaction(type, amount, newBalance));
    }

            public void displayTransactionHistory() {
        System.out.println("\n=== Transaction History ===");
        for (Transaction t : transactions) {
            t.displayTransaction();
        }
    }
            
    // Overloaded deposit method
    public void deposit(double amount, String description) {
        deposit(amount);
        System.out.println("Deposited: $" + amount + " - " + description);
    }
    
}
