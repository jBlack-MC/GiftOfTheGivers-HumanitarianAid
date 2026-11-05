package com.example.giftofthegivers_mobile.ui.components

import androidx.compose.foundation.background
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.shape.RoundedCornerShape
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.ui.Modifier
import androidx.compose.ui.draw.clip
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.compose.ui.unit.sp
import com.example.giftofthegivers_mobile.data.PaymentStatus
import com.example.giftofthegivers_mobile.ui.theme.DangerRed
import com.example.giftofthegivers_mobile.ui.theme.SuccessGreen
import com.example.giftofthegivers_mobile.ui.theme.WarningAmber

@Composable
fun PaymentStatusBadge(status: PaymentStatus) {
    val (backgroundColor, textColor, textLabel) = when (status) {
        PaymentStatus.Verified -> Triple(SuccessGreen.copy(alpha = 0.15f), SuccessGreen, "Paid")
        PaymentStatus.Pending -> Triple(WarningAmber.copy(alpha = 0.2f), Color(0xFFB7791F), "Awaiting payment")
        PaymentStatus.Failed -> Triple(DangerRed.copy(alpha = 0.15f), DangerRed, "Failed")
    }

    Box(
        modifier = Modifier
            .clip(RoundedCornerShape(6.dp))
            .background(backgroundColor)
            .padding(horizontal = 8.dp, vertical = 4.dp)
    ) {
        Text(
            text = textLabel,
            color = textColor,
            fontSize = 12.sp,
            fontWeight = FontWeight.SemiBold
        )
    }
}
