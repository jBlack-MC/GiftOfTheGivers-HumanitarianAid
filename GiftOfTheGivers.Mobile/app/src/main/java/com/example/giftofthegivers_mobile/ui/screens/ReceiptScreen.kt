package com.example.giftofthegivers_mobile.ui.screens

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.*
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.CheckCircle
import androidx.compose.material.icons.filled.Receipt
import androidx.compose.material3.*
import androidx.compose.runtime.Composable
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontFamily
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.text.style.TextAlign
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.giftofthegivers_mobile.data.Donation
import com.example.giftofthegivers_mobile.data.PaymentStatus
import com.example.giftofthegivers_mobile.ui.components.PaymentStatusBadge
import com.example.giftofthegivers_mobile.ui.theme.PrimarySubtle
import com.example.giftofthegivers_mobile.ui.theme.SuccessGreen

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ReceiptScreen(
    donation: Donation?,
    onSimulatePayFastComplete: (String) -> Unit,
    onBackHome: () -> Unit
) {
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Donation Receipt", fontWeight = FontWeight.Bold) }
            )
        }
    ) { padding ->
        if (donation == null) {
            Box(
                modifier = Modifier
                    .fillMaxSize()
                    .padding(padding),
                contentAlignment = Alignment.Center
            ) {
                Text("No recent donation found.")
            }
            return@Scaffold
        }

        Column(
            modifier = Modifier
                .fillMaxSize()
                .padding(padding)
                .padding(20.dp),
            horizontalAlignment = Alignment.CenterHorizontally
        ) {
            Spacer(modifier = Modifier.height(16.dp))

            Icon(
                imageVector = if (donation.paymentStatus == PaymentStatus.Verified) Icons.Default.CheckCircle else Icons.Default.Receipt,
                contentDescription = "Status",
                tint = if (donation.paymentStatus == PaymentStatus.Verified) SuccessGreen else MaterialTheme.colorScheme.primary,
                modifier = Modifier.size(64.dp)
            )

            Spacer(modifier = Modifier.height(12.dp))

            Text(
                text = if (donation.paymentStatus == PaymentStatus.Verified) "Payment Verified!" else "Donation Pending Payment",
                style = MaterialTheme.typography.titleLarge,
                fontWeight = FontWeight.Bold
            )

            Spacer(modifier = Modifier.height(6.dp))

            PaymentStatusBadge(status = donation.paymentStatus)

            Spacer(modifier = Modifier.height(24.dp))

            Card(
                modifier = Modifier.fillMaxWidth(),
                shape = RoundedCornerShape(12.dp),
                colors = CardDefaults.cardColors(containerColor = MaterialTheme.colorScheme.surface),
                elevation = CardDefaults.cardElevation(defaultElevation = 2.dp)
            ) {
                Column(modifier = Modifier.padding(16.dp)) {
                    ReceiptRow(label = "Reference", value = donation.reference, isMono = true)
                    Divider(modifier = Modifier.padding(vertical = 8.dp))
                    ReceiptRow(label = "Project", value = donation.projectName)
                    Divider(modifier = Modifier.padding(vertical = 8.dp))
                    ReceiptRow(label = "Amount", value = "${donation.currency} %,.2f".format(donation.amount))
                    if (donation.currency != "ZAR") {
                        Divider(modifier = Modifier.padding(vertical = 8.dp))
                        ReceiptRow(label = "Charged in Rand", value = "R %,.2f".format(donation.amountZar))
                    }
                    Divider(modifier = Modifier.padding(vertical = 8.dp))
                    ReceiptRow(label = "Donor", value = donation.donorName)
                    Divider(modifier = Modifier.padding(vertical = 8.dp))
                    ReceiptRow(label = "Date", value = donation.createdAt)
                }
            }

            Spacer(modifier = Modifier.height(24.dp))

            if (donation.paymentStatus == PaymentStatus.Pending) {
                Button(
                    onClick = { onSimulatePayFastComplete(donation.id) },
                    modifier = Modifier.fillMaxWidth(),
                    colors = ButtonDefaults.buttonColors(containerColor = SuccessGreen)
                ) {
                    Text("Simulate PayFast ITN (Complete Payment)")
                }
                Spacer(modifier = Modifier.height(12.dp))
            } else {
                Card(
                    modifier = Modifier.fillMaxWidth(),
                    colors = CardDefaults.cardColors(containerColor = PrimarySubtle)
                ) {
                    Column(modifier = Modifier.padding(12.dp)) {
                        Text(
                            text = "Section 18A Tax Certificate Available",
                            fontWeight = FontWeight.Bold,
                            color = MaterialTheme.colorScheme.primary,
                            fontSize = 13.sp
                        )
                        Text(
                            text = "Your tax deductible certificate is ready in the Donor Portal.",
                            fontSize = 12.sp,
                            color = MaterialTheme.colorScheme.onSurfaceVariant
                        )
                    }
                }
                Spacer(modifier = Modifier.height(12.dp))
            }

            OutlinedButton(
                onClick = onBackHome,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text("Return to Home")
            }
        }
    }
}

@Composable
fun ReceiptRow(label: String, value: String, isMono: Boolean = false) {
    Row(
        modifier = Modifier.fillMaxWidth(),
        horizontalArrangement = Arrangement.SpaceBetween
    ) {
        Text(text = label, color = MaterialTheme.colorScheme.onSurfaceVariant, fontSize = 13.sp)
        Text(
            text = value,
            fontWeight = FontWeight.Bold,
            fontSize = 13.sp,
            fontFamily = if (isMono) FontFamily.Monospace else FontFamily.Default
        )
    }
}
